using Azure.AI.DocumentIntelligence;

static class InvoiceMapper
{
    public static InvoiceOutput Map(IReadOnlyDictionary<string, DocumentField> f) => new(
        InvoiceId: GetString(f, "InvoiceId"),
        PurchaseOrder: GetString(f, "PurchaseOrder"),
        CustomerId: GetString(f, "CustomerId"),
        InvoiceDate: GetDate(f, "InvoiceDate"),
        DueDate: GetDate(f, "DueDate"),
        ServiceStartDate: GetDate(f, "ServiceStartDate"),
        ServiceEndDate: GetDate(f, "ServiceEndDate"),
        VendorName: GetString(f, "VendorName"),
        VendorAddress: GetAddress(f, "VendorAddress"),
        VendorAddressRecipient: GetString(f, "VendorAddressRecipient"),
        VendorTaxId: GetString(f, "VendorTaxId"),
        CustomerName: GetString(f, "CustomerName"),
        CustomerAddress: GetAddress(f, "CustomerAddress"),
        CustomerAddressRecipient: GetString(f, "CustomerAddressRecipient"),
        CustomerTaxId: GetString(f, "CustomerTaxId"),
        BillingAddress: GetAddress(f, "BillingAddress"),
        BillingAddressRecipient: GetString(f, "BillingAddressRecipient"),
        ShippingAddress: GetAddress(f, "ShippingAddress"),
        ShippingAddressRecipient: GetString(f, "ShippingAddressRecipient"),
        ServiceAddress: GetAddress(f, "ServiceAddress"),
        ServiceAddressRecipient: GetString(f, "ServiceAddressRecipient"),
        RemittanceAddress: GetAddress(f, "RemittanceAddress"),
        RemittanceAddressRecipient: GetString(f, "RemittanceAddressRecipient"),
        PaymentTerm: GetString(f, "PaymentTerm"),
        KVKNumber: GetString(f, "KVKNumber"),
        SubTotal: GetCurrency(f, "SubTotal"),
        TotalDiscount: GetCurrency(f, "TotalDiscount"),
        TotalTax: GetCurrency(f, "TotalTax"),
        InvoiceTotal: GetCurrency(f, "InvoiceTotal"),
        AmountDue: GetCurrency(f, "AmountDue"),
        PreviousUnpaidBalance: GetCurrency(f, "PreviousUnpaidBalance"),
        Items: GetItems(f),
        PaymentDetails: GetPaymentDetails(f),
        TaxDetails: GetTaxDetails(f)
    );

    private static string? GetString(IReadOnlyDictionary<string, DocumentField> f, string key)
        => f.TryGetValue(key, out var field) ? field.ValueString : null;

    private static double? GetCurrency(IReadOnlyDictionary<string, DocumentField> f, string key)
        => f.TryGetValue(key, out var field) ? field.ValueCurrency?.Amount : null;

    private static DateTimeOffset? GetDate(IReadOnlyDictionary<string, DocumentField> f, string key)
        => f.TryGetValue(key, out var field) ? field.ValueDate : null;

    private static string? GetAddress(IReadOnlyDictionary<string, DocumentField> f, string key)
    {
        if (!f.TryGetValue(key, out var field) || field.ValueAddress is not { } a)
            return null;
        return string.Join(", ", new[] { a.StreetAddress, a.City, a.State, a.PostalCode, a.CountryRegion }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static List<InvoiceLineItem> GetItems(IReadOnlyDictionary<string, DocumentField> f)
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

    private static List<InvoicePaymentDetail> GetPaymentDetails(IReadOnlyDictionary<string, DocumentField> f)
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

    private static List<InvoiceTaxDetail> GetTaxDetails(IReadOnlyDictionary<string, DocumentField> f)
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
}
