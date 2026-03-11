using System.Collections.Generic;
using System;

namespace Domain.Interfaces;

public interface ISmartSearchService
{
    /// <summary>
    /// Filters and ranks a collection of items based on a search term using fuzzy matching.
    /// </summary>
    /// <typeparam name="T">The type of items to search.</typeparam>
    /// <param name="items">The collection of items to search.</param>
    /// <param name="searchTerm">The search term provided by the user.</param>
    /// <param name="selector">A function that extracts the searchable string from an item.</param>
    /// <param name="threshold">Minimum similarity score (0.0 to 1.0) to include an item.</param>
    /// <returns>A collection of items ranked by relevance.</returns>
    IEnumerable<T> Search<T>(IEnumerable<T> items, string searchTerm, Func<T, string> selector, double threshold = 0.3);

    /// <summary>
    /// Calculates the similarity score between two strings (0.0 to 1.0).
    /// </summary>
    double CalculateSimilarity(string source, string target);
}
