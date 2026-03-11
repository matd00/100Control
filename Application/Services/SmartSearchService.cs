using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Interfaces.Services;

namespace Application.Services;

public class SmartSearchService : ISmartSearchService
{
    public IEnumerable<T> Search<T>(IEnumerable<T> items, string searchTerm, Func<T, string> selector, double threshold = 0.3)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return items;

        var term = searchTerm.Trim().ToLowerInvariant();

        return items
            .Select(item => new { Item = item, Score = CalculateSimilarity(term, selector(item).ToLowerInvariant()) })
            .Where(x => x.Score >= threshold || selector(x.Item).ToLowerInvariant().Contains(term))
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item);
    }

    public double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        if (source == target)
            return 1.0;

        // Simple contains check (higher weight)
        if (target.Contains(source))
            return 0.8 + (double)source.Length / target.Length * 0.2;

        int n = source.Length;
        int m = target.Length;
        int[,] distance = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) distance[i, 0] = i;
        for (int j = 0; j <= m; j++) distance[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        int maxLen = Math.Max(n, m);
        return 1.0 - (double)distance[n, m] / maxLen;
    }
}
