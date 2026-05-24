using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: InvoiceReader <invoice-file-path>");
    return 1;
}

string inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"File not found: {inputPath}");
    return 1;
}

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

string endpoint = config["DocumentIntelligence:Endpoint"]
    ?? throw new InvalidOperationException("DocumentIntelligence:Endpoint is not configured.");
string apiKey = config["DocumentIntelligence:ApiKey"]
    ?? throw new InvalidOperationException("DocumentIntelligence:ApiKey is not configured.");

var client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

Console.WriteLine($"Analyzing: {inputPath}");

byte[] fileBytes = await File.ReadAllBytesAsync(inputPath);
var options = new AnalyzeDocumentOptions("prebuilt-invoice", BinaryData.FromBytes(fileBytes));
var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
var result = operation.Value;

if (result.Documents.Count == 0)
{
    Console.Error.WriteLine("No invoice document detected in the file.");
    return 1;
}

var fields = result.Documents[0].Fields;

var invoice = new InvoiceOutput(
    InvoiceId: GetString(fields, "InvoiceId"),
    PurchaseOrder: GetString(fields, "PurchaseOrder"),
    CustomerId: GetString(fields, "CustomerId"),
    InvoiceDate: GetDate(fields, "InvoiceDate"),
    DueDate: GetDate(fields, "DueDate"),
    ServiceStartDate: GetDate(fields, "ServiceStartDate"),
    ServiceEndDate: GetDate(fields, "ServiceEndDate"),
    VendorName: GetString(fields, "VendorName"),
    VendorAddress: GetAddress(fields, "VendorAddress"),
    VendorAddressRecipient: GetString(fields, "VendorAddressRecipient"),
    VendorTaxId: GetString(fields, "VendorTaxId"),
    CustomerName: GetString(fields, "CustomerName"),
    CustomerAddress: GetAddress(fields, "CustomerAddress"),
    CustomerAddressRecipient: GetString(fields, "CustomerAddressRecipient"),
    CustomerTaxId: GetString(fields, "CustomerTaxId"),
    BillingAddress: GetAddress(fields, "BillingAddress"),
    BillingAddressRecipient: GetString(fields, "BillingAddressRecipient"),
    ShippingAddress: GetAddress(fields, "ShippingAddress"),
    ShippingAddressRecipient: GetString(fields, "ShippingAddressRecipient"),
    ServiceAddress: GetAddress(fields, "ServiceAddress"),
    ServiceAddressRecipient: GetString(fields, "ServiceAddressRecipient"),
    RemittanceAddress: GetAddress(fields, "RemittanceAddress"),
    RemittanceAddressRecipient: GetString(fields, "RemittanceAddressRecipient"),
    PaymentTerm: GetString(fields, "PaymentTerm"),
    KVKNumber: GetString(fields, "KVKNumber"),
    SubTotal: GetCurrency(fields, "SubTotal"),
    TotalDiscount: GetCurrency(fields, "TotalDiscount"),
    TotalTax: GetCurrency(fields, "TotalTax"),
    InvoiceTotal: GetCurrency(fields, "InvoiceTotal"),
    AmountDue: GetCurrency(fields, "AmountDue"),
    PreviousUnpaidBalance: GetCurrency(fields, "PreviousUnpaidBalance"),
    Items: GetItems(fields),
    PaymentDetails: GetPaymentDetails(fields),
    TaxDetails: GetTaxDetails(fields)
);

string stem = Path.GetFileNameWithoutExtension(inputPath);
string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{stem}_result.json");

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(invoice, jsonOptions));
Console.WriteLine($"Result saved to: {outputPath}");
return 0;

// --- field helpers ---

string? GetString(IReadOnlyDictionary<string, DocumentField> f, string key)
    => f.TryGetValue(key, out var field) ? field.ValueString : null;

double? GetCurrency(IReadOnlyDictionary<string, DocumentField> f, string key)
    => f.TryGetValue(key, out var field) ? field.ValueCurrency?.Amount : null;

DateTimeOffset? GetDate(IReadOnlyDictionary<string, DocumentField> f, string key)
    => f.TryGetValue(key, out var field) ? field.ValueDate : null;

string? GetAddress(IReadOnlyDictionary<string, DocumentField> f, string key)
{
    if (!f.TryGetValue(key, out var field) || field.ValueAddress is not { } a)
        return null;
    return string.Join(", ", new[] { a.StreetAddress, a.City, a.State, a.PostalCode, a.CountryRegion }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}

List<InvoiceLineItem> GetItems(IReadOnlyDictionary<string, DocumentField> f)
{
    if (!f.TryGetValue("Items", out var field) || field.ValueList is not { } list)
        return [];
    return list
        .Select(item => item.ValueDictionary is { } obj ? new InvoiceLineItem(
            Description: GetString(obj, "Description"),
            ProductCode: GetString(obj, "ProductCode"),
            Quantity: obj.TryGetValue("Quantity", out var q) ? q.ValueDouble : null,
            Unit: GetString(obj, "Unit"),
            UnitPrice: GetCurrency(obj, "UnitPrice"),
            Amount: GetCurrency(obj, "Amount"),
            Tax: GetCurrency(obj, "Tax"),
            TaxRate: GetString(obj, "TaxRate"),
            Date: GetDate(obj, "Date")
        ) : null)
        .OfType<InvoiceLineItem>()
        .ToList();
}

List<InvoicePaymentDetail> GetPaymentDetails(IReadOnlyDictionary<string, DocumentField> f)
{
    if (!f.TryGetValue("PaymentDetails", out var field) || field.ValueList is not { } list)
        return [];
    return list
        .Select(item => item.ValueDictionary is { } obj ? new InvoicePaymentDetail(
            BankAccountNumber: GetString(obj, "BankAccountNumber"),
            IBAN: GetString(obj, "IBAN"),
            SWIFT: GetString(obj, "SWIFT"),
            RoutingNumber: GetString(obj, "RoutingNumber"),
            PaymentDate: GetDate(obj, "PaymentDate")
        ) : null)
        .OfType<InvoicePaymentDetail>()
        .ToList();
}

List<InvoiceTaxDetail> GetTaxDetails(IReadOnlyDictionary<string, DocumentField> f)
{
    if (!f.TryGetValue("TaxDetails", out var field) || field.ValueList is not { } list)
        return [];
    return list
        .Select(item => item.ValueDictionary is { } obj ? new InvoiceTaxDetail(
            Amount: GetCurrency(obj, "Amount"),
            TaxRate: GetString(obj, "TaxRate")
        ) : null)
        .OfType<InvoiceTaxDetail>()
        .ToList();
}

// --- DTOs ---

record InvoiceOutput(
    string? InvoiceId,
    string? PurchaseOrder,
    string? CustomerId,
    DateTimeOffset? InvoiceDate,
    DateTimeOffset? DueDate,
    DateTimeOffset? ServiceStartDate,
    DateTimeOffset? ServiceEndDate,
    string? VendorName,
    string? VendorAddress,
    string? VendorAddressRecipient,
    string? VendorTaxId,
    string? CustomerName,
    string? CustomerAddress,
    string? CustomerAddressRecipient,
    string? CustomerTaxId,
    string? BillingAddress,
    string? BillingAddressRecipient,
    string? ShippingAddress,
    string? ShippingAddressRecipient,
    string? ServiceAddress,
    string? ServiceAddressRecipient,
    string? RemittanceAddress,
    string? RemittanceAddressRecipient,
    string? PaymentTerm,
    string? KVKNumber,
    double? SubTotal,
    double? TotalDiscount,
    double? TotalTax,
    double? InvoiceTotal,
    double? AmountDue,
    double? PreviousUnpaidBalance,
    List<InvoiceLineItem> Items,
    List<InvoicePaymentDetail> PaymentDetails,
    List<InvoiceTaxDetail> TaxDetails
);

record InvoiceLineItem(
    string? Description,
    string? ProductCode,
    double? Quantity,
    string? Unit,
    double? UnitPrice,
    double? Amount,
    double? Tax,
    string? TaxRate,
    DateTimeOffset? Date
);

record InvoicePaymentDetail(
    string? BankAccountNumber,
    string? IBAN,
    string? SWIFT,
    string? RoutingNumber,
    DateTimeOffset? PaymentDate
);

record InvoiceTaxDetail(
    double? Amount,
    string? TaxRate
);
