using System;
using System.Collections.Generic;
using System.Linq;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Fuzzy string matching için Levenshtein distance algoritması
    /// </summary>
    public static class FuzzySearchService
    {
        /// <summary>
        /// İki string arasındaki Levenshtein mesafesini hesaplar
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Benzerlik oranını hesaplar (0-1 arası, 1 = tam eşleşme)
        /// </summary>
        public static double SimilarityRatio(string s, string t)
        {
            if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(t)) return 1.0;
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 0.0;

            int maxLen = Math.Max(s.Length, t.Length);
            if (maxLen == 0) return 1.0;

            int distance = LevenshteinDistance(s, t);
            return 1.0 - (double)distance / maxLen;
        }

        /// <summary>
        /// Fuzzy search - eşik değeri üzerinde eşleşen sonuçları döndürür
        /// </summary>
        public static bool IsFuzzyMatch(string source, string target, double threshold = 0.6)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return false;

            // Tam eşleşme kontrolü
            if (source.Contains(target, StringComparison.OrdinalIgnoreCase))
                return true;

            // Fuzzy eşleşme kontrolü
            double similarity = SimilarityRatio(source.ToLower(), target.ToLower());
            return similarity >= threshold;
        }

        /// <summary>
        /// Metin içinde kelime kelime arama yapar
        /// </summary>
        public static bool ContainsFuzzy(string text, string searchTerm, double threshold = 0.6)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
                return false;

            var textWords = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var searchWords = searchTerm.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var searchWord in searchWords)
            {
                bool found = false;
                foreach (var textWord in textWords)
                {
                    if (IsFuzzyMatch(textWord, searchWord, threshold))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Gelişmiş filtreleme kriterleri
    /// </summary>
    public class StudentFilterCriteria
    {
        public string? SearchText { get; set; }
        public string? BookletType { get; set; }
        public double? MinScore { get; set; }
        public double? MaxScore { get; set; }
        public int? MinCorrect { get; set; }
        public int? MaxCorrect { get; set; }
        public bool? HasEmptyAnswers { get; set; }
        public bool UseFuzzySearch { get; set; } = true;
        public double FuzzyThreshold { get; set; } = 0.6;
    }
}
