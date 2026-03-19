using System.Text.RegularExpressions;
using EntityFrameworkCore.ManagedViews.Configuration;

namespace EntityFrameworkCore.ManagedViews.Hashing;

/// <summary>
/// Normalizes SQL text before hashing to prevent whitespace-only changes
/// from generating unnecessary migrations.
/// </summary>
public sealed partial class SqlNormalizer
{
    [GeneratedRegex(@"--(?!.*@\w+:).*$", RegexOptions.Multiline)]
    private static partial Regex SingleLineCommentRegex();

    [GeneratedRegex(@"/\*[\s\S]*?\*/")]
    private static partial Regex MultiLineCommentRegex();

    [GeneratedRegex(@"^--\s*@\w+:.*$", RegexOptions.Multiline)]
    private static partial Regex MetadataDirectiveRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();

    /// <summary>
    /// Normalizes SQL by stripping directives, optionally stripping comments,
    /// collapsing whitespace, and trimming.
    /// </summary>
#pragma warning disable CA1822 // Mark members as static - used via instance in Sha256ViewHasher
    public string Normalize(string sql, ManagedViewOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string result = sql;

        result = MetadataDirectiveRegex().Replace(result, "");

        if (options.StripCommentsBeforeHashing)
        {
            result = SingleLineCommentRegex().Replace(result, "");
            result = MultiLineCommentRegex().Replace(result, " ");
        }

        result = MultipleWhitespaceRegex().Replace(result, " ");

        result = result.Trim().TrimEnd(';').Trim();

        return result;
    }
#pragma warning restore CA1822
}
