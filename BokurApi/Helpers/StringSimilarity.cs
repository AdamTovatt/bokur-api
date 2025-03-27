namespace BokurApi.Helpers
{
    public static class StringSimilarity
    {
        public static string RemoveDuplicateWords(string input)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> result = new List<string>();

            foreach (string word in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (seen.Add(word))
                {
                    result.Add(word);
                }
            }

            return string.Join(' ', result);
        }

        public static double CosineSimilarity(string s1, string s2, bool ignoreDuplicateWords = false)
        {
            if (ignoreDuplicateWords)
            {
                (string, string) withoutOutliers = RemoveSmallOutlierWords(s1, s2);

                s1 = RemoveDuplicateWords(withoutOutliers.Item1);
                s2 = RemoveDuplicateWords(withoutOutliers.Item2);
            }

            Dictionary<string, int> vec1 = GetTermFrequencies(s1);
            Dictionary<string, int> vec2 = GetTermFrequencies(s2);

            HashSet<string> allTokens = new HashSet<string>(vec1.Keys);
            allTokens.UnionWith(vec2.Keys);

            double dotProduct = allTokens.Sum(token => vec1.GetValueOrDefault(token) * vec2.GetValueOrDefault(token));
            double magnitude1 = Math.Sqrt(vec1.Values.Sum(v => v * v));
            double magnitude2 = Math.Sqrt(vec2.Values.Sum(v => v * v));

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0.0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        private static Dictionary<string, int> GetTermFrequencies(string input)
        {
            return input
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .GroupBy(token => token)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private static (string, string) RemoveSmallOutlierWords(string a, string b, int maxLevenshteinDistance = 1)
        {
            List<string> wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            List<string> matchedA = new List<string>();
            List<string> matchedB = new List<string>();

            foreach (string wordA in wordsA)
            {
                bool hasMatch = wordsB.Any(wordB => LevenshteinDistance(wordA, wordB) <= maxLevenshteinDistance);
                if (hasMatch || wordA.Length >= 4)
                    matchedA.Add(wordA);
            }

            foreach (string wordB in wordsB)
            {
                bool hasMatch = wordsA.Any(wordA => LevenshteinDistance(wordB, wordA) <= maxLevenshteinDistance);
                if (hasMatch || wordB.Length >= 4)
                    matchedB.Add(wordB);
            }

            int maxOutliersA = (int)Math.Ceiling(wordsA.Count * 0.1);
            int maxOutliersB = (int)Math.Ceiling(wordsB.Count * 0.1);

            if (wordsA.Count - matchedA.Count <= maxOutliersA && wordsB.Count - matchedB.Count <= maxOutliersB)
            {
                return (string.Join(' ', matchedA), string.Join(' ', matchedB));
            }

            return (a, b);
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s.Length, t.Length];
        }
    }
}
