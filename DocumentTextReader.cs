using Azure;
using Azure.AI.DocumentIntelligence;

class DocumentTextReader : ITextReader
{
    private readonly DocumentIntelligenceClient _client;

    public DocumentTextReader(string endpoint, string apiKey)
        => _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

    public async Task<string> ReadTextAsync(string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        var options = new AnalyzeDocumentOptions("prebuilt-read", BinaryData.FromBytes(fileBytes));
        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
        return string.Join(" ", operation.Value.Pages
            .SelectMany(p => p.Lines)
            .Select(l => l.Content));
    }
}
