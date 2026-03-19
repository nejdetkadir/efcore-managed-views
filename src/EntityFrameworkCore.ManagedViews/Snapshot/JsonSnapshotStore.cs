using System.Text.Json;
using System.Text.Json.Serialization;
using EntityFrameworkCore.ManagedViews.Abstractions;

namespace EntityFrameworkCore.ManagedViews.Snapshot;

/// <summary>
/// JSON-based snapshot store that reads/writes the ManagedViewsSnapshot.json file
/// in the migrations directory.
/// </summary>
public sealed class JsonSnapshotStore : IManagedViewSnapshotStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public ManagedViewSnapshot? Load(string migrationsDirectory, string contextName)
    {
        string filePath = GetSnapshotPath(migrationsDirectory, contextName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ManagedViewSnapshot>(json, _jsonOptions);
    }

    /// <inheritdoc />
    public void Save(ManagedViewSnapshot snapshot, string migrationsDirectory, string contextName)
    {
        string filePath = GetSnapshotPath(migrationsDirectory, contextName);
        string? directory = Path.GetDirectoryName(filePath);

        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(snapshot, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static string GetSnapshotPath(string migrationsDirectory, string contextName)
    {
        return Path.Combine(migrationsDirectory, $"{contextName}ManagedViewsSnapshot.json");
    }
}
