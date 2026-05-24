using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: InvoiceReader <invoice-file-path> [--search <phrase>]");
    return 1;
}

string inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"File not found: {inputPath}");
    return 1;
}

string? searchPhrase = null;
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--search" && i + 1 < args.Length)
        searchPhrase = args[++i];
    else
    {
        Console.Error.WriteLine($"Unknown argument: {args[i]}");
        Console.Error.WriteLine("Usage: InvoiceReader <invoice-file-path> [--search <phrase>]");
        return 1;
    }
}

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

string endpoint = config["DocumentIntelligence:Endpoint"]
    ?? throw new InvalidOperationException("DocumentIntelligence:Endpoint is not configured.");
string apiKey = config["DocumentIntelligence:ApiKey"]
    ?? throw new InvalidOperationException("DocumentIntelligence:ApiKey is not configured.");

IInvoiceAnalyzer analyzer = new InvoiceAnalyzer(endpoint, apiKey);

Console.WriteLine($"Analyzing: {inputPath}");

InvoiceOutput invoice;
try
{
    invoice = await analyzer.AnalyzeAsync(inputPath);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

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

if (searchPhrase is not null)
{
    Console.WriteLine($"Searching for: \"{searchPhrase}\"");
    ITextReader textReader = new DocumentTextReader(endpoint, apiKey);
    string text = await textReader.ReadTextAsync(inputPath);
    var (found, score) = PhraseSearcher.Search(text, searchPhrase);
    Console.WriteLine(found
        ? $"Found (match score: {score}/100)"
        : $"Not found (best score: {score}/100, threshold: 80)");
}

return 0;

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
