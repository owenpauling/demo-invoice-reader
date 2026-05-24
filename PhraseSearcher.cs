using FuzzySharp;

static class PhraseSearcher
{
    private const int Threshold = 80;

    public static (bool found, int score) Search(string text, string phrase)
    {
        int score = Fuzz.PartialRatio(phrase.ToLowerInvariant(), text.ToLowerInvariant());
        return (score >= Threshold, score);
    }
}
