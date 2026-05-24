static class PhraseSearcher
{
    private const int Threshold = 80;

    public static (bool found, int score) Search(string text, string phrase)
    {
        string normalizedText = Normalize(text);
        string normalizedPhrase = Normalize(phrase);

        if (normalizedPhrase.Length == 0 || normalizedText.Length < normalizedPhrase.Length)
            return (false, 0);

        int bestScore = 0;
        int windowLen = normalizedPhrase.Length;

        for (int i = 0; i <= normalizedText.Length - windowLen; i++)
        {
            int distance = LevenshteinDistance(normalizedPhrase, normalizedText.Substring(i, windowLen));
            int score = (int)Math.Round((1.0 - (double)distance / windowLen) * 100);
            if (score > bestScore) bestScore = score;
            if (bestScore == 100) break;
        }

        return (bestScore >= Threshold, bestScore);
    }

    private static string Normalize(string s) =>
        string.Join(" ", s.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        int[] prev = new int[n + 1];
        int[] curr = new int[n + 1];

        for (int j = 0; j <= n; j++) prev[j] = j;

        for (int i = 1; i <= m; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= n; j++)
            {
                curr[j] = a[i - 1] == b[j - 1]
                    ? prev[j - 1]
                    : 1 + Math.Min(prev[j - 1], Math.Min(prev[j], curr[j - 1]));
            }
            (prev, curr) = (curr, prev);
        }

        return prev[n];
    }
}
