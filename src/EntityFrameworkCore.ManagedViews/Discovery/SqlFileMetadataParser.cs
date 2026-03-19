using System.Text.RegularExpressions;
using EntityFrameworkCore.ManagedViews.Exceptions;
using EntityFrameworkCore.ManagedViews.Models;

namespace EntityFrameworkCore.ManagedViews.Discovery;

/// <summary>
/// Parses .sql files to extract view metadata from header comments and the SQL body.
/// </summary>
#pragma warning disable CA1308 // ToLowerInvariant used intentionally for directive key normalization
public sealed partial class SqlFileMetadataParser
{
    [GeneratedRegex(@"^--\s*@(\w+):\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex DirectivePatternRegex();

    /// <summary>
    /// Parses a .sql file content into a <see cref="ManagedViewDefinition"/>.
    /// </summary>
    /// <param name="content">Full content of the .sql file.</param>
    /// <param name="fileName">Filename for fallback viewName inference.</param>
    /// <param name="defaultSchema">Default schema from options.</param>
    /// <param name="source">Source identifier for diagnostics.</param>
    public static ManagedViewDefinition Parse(
        string content,
        string fileName,
        string defaultSchema,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var directives = ExtractDirectives(content);
        string sqlBody = ExtractSqlBody(content);

        if (string.IsNullOrWhiteSpace(sqlBody))
        {
            throw new ManagedViewSqlParseException(
                $"No SQL body found in '{source}'. The file must contain a SELECT statement.");
        }

        string viewName = directives.GetValueOrDefault("viewname")
            ?? InferViewNameFromFileName(fileName);

        string schema = directives.GetValueOrDefault("schema")
            ?? InferSchemaFromFileName(fileName)
            ?? defaultSchema;

        ManagedViewType viewType = directives.GetValueOrDefault("type")?.ToLowerInvariant() switch
        {
            "materialized" => ManagedViewType.Materialized,
            "view" or null => ManagedViewType.View,
            var unknown => throw new ManagedViewSqlParseException(
                $"Unknown view type '{unknown}' in '{source}'. Expected 'view' or 'materialized'.")
        };

        List<string> dependsOn = directives.GetValueOrDefault("dependson")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? [];

        List<ManagedViewIndex> indexes = ParseIndexes(directives.GetValueOrDefault("indexes"));

        return new ManagedViewDefinition
        {
            ViewName = viewName,
            Schema = schema,
            ViewType = viewType,
            Sql = sqlBody,
            DependsOn = dependsOn,
            Indexes = indexes,
            Source = source
        };
    }

    private static Dictionary<string, string> ExtractDirectives(string content)
    {
        var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in DirectivePatternRegex().Matches(content))
        {
            string key = match.Groups[1].Value.ToLowerInvariant();
            string value = match.Groups[2].Value.Trim();
            directives[key] = value;
        }

        return directives;
    }

    private static string ExtractSqlBody(string content)
    {
        var lines = content.Split('\n')
            .Where(line => !DirectivePatternRegex().IsMatch(line))
            .ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join('\n', lines).Trim().TrimEnd(';');
    }

    private static string InferViewNameFromFileName(string fileName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string[] parts = nameWithoutExtension.Split('.');
        return parts.Length > 1 ? parts[^1] : nameWithoutExtension;
    }

    private static string? InferSchemaFromFileName(string fileName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string[] parts = nameWithoutExtension.Split('.');
        return parts.Length > 1 ? parts[0] : null;
    }

    private static List<ManagedViewIndex> ParseIndexes(string? indexesDirective)
    {
        if (string.IsNullOrWhiteSpace(indexesDirective))
        {
            return [];
        }

        return indexesDirective
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseSingleIndex)
            .ToList();
    }

    private static ManagedViewIndex ParseSingleIndex(string indexDef)
    {
#pragma warning disable CA1307 // LastIndexOf(char, StringComparison) not available; char search is culture-invariant
        int parenStart = indexDef.IndexOf('(', StringComparison.Ordinal);
        int parenEnd = indexDef.LastIndexOf(')');
#pragma warning restore CA1307

        if (parenStart < 0 || parenEnd < 0 || parenEnd <= parenStart)
        {
            throw new ManagedViewSqlParseException(
                $"Invalid index definition: '{indexDef}'. Expected format: 'index_name(columns)'.");
        }

        string name = indexDef[..parenStart].Trim();
        string columns = indexDef[(parenStart + 1)..parenEnd].Trim();

        return new ManagedViewIndex(name, columns);
    }
}
