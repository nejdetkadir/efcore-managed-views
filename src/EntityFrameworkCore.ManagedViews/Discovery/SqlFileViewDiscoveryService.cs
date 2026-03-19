using System.Reflection;
using EntityFrameworkCore.ManagedViews.Abstractions;
using EntityFrameworkCore.ManagedViews.Configuration;

namespace EntityFrameworkCore.ManagedViews.Discovery;

/// <summary>
/// Discovers view definitions from embedded .sql resource files.
/// </summary>
public sealed class SqlFileViewDiscoveryService : IManagedViewDiscovery
{
    /// <inheritdoc />
    public IReadOnlyList<IManagedViewDefinition> Discover(ManagedViewOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Assembly assembly = options.ViewAssembly
            ?? Assembly.GetEntryAssembly()
            ?? Assembly.GetCallingAssembly();

        string[] resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(options.SqlFilesPrefix, StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var definitions = new List<IManagedViewDefinition>();

        foreach (string resourceName in resourceNames)
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            string fileName = ExtractFileName(resourceName);
            var definition = SqlFileMetadataParser.Parse(content, fileName, options.DefaultSchema, resourceName);
            definitions.Add(definition);
        }

        return definitions;
    }

    internal static string ExtractFileName(string resourceName)
    {
        int lastDotBeforeSql = resourceName.LastIndexOf('.', resourceName.Length - 5);
        if (lastDotBeforeSql >= 0)
        {
            return resourceName[(lastDotBeforeSql + 1)..];
        }
        return resourceName;
    }
}
