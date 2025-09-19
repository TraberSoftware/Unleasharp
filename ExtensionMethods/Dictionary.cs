using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp;

public static class DictionaryExtensions {
    /// <summary>
    /// Gets the value for a key, or default if key doesn't exist
    /// </summary>
    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) {
        return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
    }

    public static Dictionary<string, object> ToDictionary(
        object obj,
        bool   includeNullValues = true,
        bool   includeEmptyStrings = true
    ) {
        if (obj == null)
            return new Dictionary<string, object>();

        var dictionary = new Dictionary<string, object>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var property in properties) {
            if (property.GetIndexParameters().Length > 0)
                continue; // Skip indexers

            try {
                var value = property.GetValue(obj);

                // Apply filtering rules
                if (!includeNullValues && value == null)
                    continue;

                if (!includeEmptyStrings && value is string str && string.IsNullOrEmpty(str))
                    continue;

                dictionary[property.Name] = value;
            }
            catch (Exception ex) {
                dictionary[property.Name] = $"Error: {ex.Message}";
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Compares two dictionaries and returns items from the second dictionary
    /// that are different from the first or don't exist in the first.
    /// </summary>
    /// <param name="first">The first dictionary to compare</param>
    /// <param name="second">The second dictionary to compare against</param>
    /// <returns>A new dictionary containing items from second that differ or don't exist in first</returns>
    public static Dictionary<string, object> CompareWith(this Dictionary<string, object> first,
        Dictionary<string, object> second) {
        // Handle null inputs
        if (second == null)
            return new Dictionary<string, object>();

        if (first == null)
            return new Dictionary<string, object>(second);

        var result = new Dictionary<string, object>();

        // Check each item in the second dictionary
        foreach (var kvp in second) {
            // If key doesn't exist in first dictionary, include it
            if (!first.ContainsKey(kvp.Key)) {
                result[kvp.Key] = kvp.Value;
                continue;
            }

            // If key exists in both dictionaries, compare values
            var firstValue = first[kvp.Key];

            // Handle null values properly
            if (firstValue == null && kvp.Value == null)
                continue; // Both null, no change needed

            if (firstValue == null || kvp.Value == null) {
                // One is null, the other isn't - they're different
                result[kvp.Key] = kvp.Value;
                continue;
            }

            // Both values are non-null, compare them
            if (!firstValue.Equals(kvp.Value)) {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }
}
