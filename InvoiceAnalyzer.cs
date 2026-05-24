using Azure;
using Azure.AI.DocumentIntelligence;

class InvoiceAnalyzer : IInvoiceAnalyzer
{
    private readonly DocumentIntelligenceClient _client;

    public InvoiceAnalyzer(string endpoint, string apiKey)
    {
        _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<InvoiceOutput> AnalyzeAsync(string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        var options = new AnalyzeDocumentOptions("prebuilt-invoice", BinaryData.FromBytes(fileBytes));
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
        var result = operation.Value;

        if (result.Documents.Count == 0)
            throw new InvalidOperationException("No invoice document detected in the file.");

        return InvoiceMapper.Map(result.Documents[0].Fields);
    }
}
