interface IInvoiceAnalyzer
{
    Task<InvoiceOutput> AnalyzeAsync(string filePath);
}
