interface ITextReader
{
    Task<string> ReadTextAsync(string filePath);
}
