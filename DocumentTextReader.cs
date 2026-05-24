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
        var result = operation.Value;

        // Extract only spans the model identified as handwritten, so searches don't
        // match against printed invoice data or product descriptions.
        var handwrittenSpans = result.Styles
            .Where(s => s.IsHandwritten == true)
            .SelectMany(s => s.Spans)
            .OrderBy(s => s.Offset);

        return string.Join(" ", handwrittenSpans
            .Select(s => result.Content.Substring(s.Offset, s.Length).Trim())
            .Where(t => t.Length > 0));
    }
}
