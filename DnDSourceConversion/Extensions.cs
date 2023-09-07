using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DnDSourceConversion;

public static class StringExtensions
{
    public static (string LeftSize, string RightSide) SplitAt(this string text, char separator)
    {
        ArgumentNullException.ThrowIfNull(separator, nameof(separator));
        
        string[] split = text.Split(separator, 2);

        if (split.Length != 2)
            throw new InvalidOperationException("No separator found in string.");
        
        return (split[0], split[1]);
    }

    public static (string LeftSize, string RightSide) SplitAt(this string text, string separator)
    {
        ArgumentNullException.ThrowIfNull(separator, nameof(separator));
        
        string[] split = text.Split(separator, 2);

        if (split.Length != 2)
            throw new InvalidOperationException("No separator found in string.");
        
        return (split[0], split[1]);
    }

    
    public static string Until(this string text, char separator)
    {
        ArgumentNullException.ThrowIfNull(separator, nameof(separator));
        
        int index = text.IndexOf(separator);
        return text[..index];
    }
    
    public static string Until(this string text, string separator)
    {
        ArgumentNullException.ThrowIfNull(separator, nameof(separator));
        
        int index = text.IndexOf(separator);
        return text[..index];
    }
    
    public static string Join(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
    
    public static string Join(this IEnumerable<string> strings, string separator, string lastSeparator)
    {
        using IEnumerator<string?> en = strings.GetEnumerator();

        if (!en.MoveNext())
            return string.Empty;

        string? firstValue = en.Current;

        if (!en.MoveNext())
        {
            // Only one value available
            return firstValue ?? string.Empty;
        }

        // Null separator and values are handled by the StringBuilder
        var result = new StringBuilder();

        result.Append(firstValue);

        do
        {
            result.Append(separator);
            result.Append(en.Current);
        }
        while (en.MoveNext());

        return result.ToString();
    }
    
    public static string ToUpperFirstLetter(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        char[] a = text.ToCharArray();
        a[0] = char.ToUpper(a[0]);
        return new string(a);
    }
}

public static class JsonArrayExtensions
{
    public static string SeparatedList(this JsonArray jsonArray, string separator) => string.Join(separator, jsonArray.Select(e => e.ToString()).ToArray());
    
    public static string SeparatedList(this JsonArray jsonArray, char separator) => string.Join(separator, jsonArray.Select(e => e.ToString()).ToArray());
}