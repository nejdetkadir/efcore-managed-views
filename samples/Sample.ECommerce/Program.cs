using System.Reflection;
using EntityFrameworkCore.ManagedViews.Configuration;
using EntityFrameworkCore.ManagedViews.DependencyResolution;
using EntityFrameworkCore.ManagedViews.Diffing;
using EntityFrameworkCore.ManagedViews.Discovery;
using EntityFrameworkCore.ManagedViews.Hashing;
using EntityFrameworkCore.ManagedViews.Models;
using EntityFrameworkCore.ManagedViews.PostgreSQL;
using EntityFrameworkCore.ManagedViews.Snapshot;
using EntityFrameworkCore.ManagedViews.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sample.ECommerce;

public static class Program
{
    public static void Main()
    {
        var options = new ManagedViewOptions
        {
            ViewAssembly = Assembly.GetExecutingAssembly()
        };
        var managedViewOptions = Options.Create(options);

        Console.WriteLine("=== EntityFrameworkCore.ManagedViews - Sample E-Commerce ===");
        Console.WriteLine();

        // 1. Discover views from embedded SQL files
        Console.WriteLine("--- 1. View Discovery ---");
        var discovery = new SqlFileViewDiscoveryService();
        var views = discovery.Discover(options);

        foreach (var view in views)
        {
            Console.WriteLine($"  [{view.ViewType}] {view.Schema}.{view.ViewName}");
            Console.WriteLine($"    Source: {view.Source}");
            if (view.DependsOn.Count > 0)
            {
                Console.WriteLine($"    Depends on: {string.Join(", ", view.DependsOn)}");
            }

            if (view.Indexes.Count > 0)
            {
                Console.WriteLine($"    Indexes: {string.Join(", ", view.Indexes.Select(i => $"{i.Name}({i.Columns})"))}");
            }
            Console.WriteLine();
        }

        // 2. Resolve dependency order
        Console.WriteLine("--- 2. Dependency Resolution ---");
        var resolver = new TopologicalSortResolver();
        var createOrder = resolver.ResolveCreateOrder(views);

        Console.WriteLine("  CREATE order:");
        for (int i = 0; i < createOrder.Count; i++)
        {
            Console.WriteLine($"    {i + 1}. {createOrder[i].Schema}.{createOrder[i].ViewName}");
        }

        var dropOrder = resolver.ResolveDropOrder(views);
        Console.WriteLine("  DROP order:");
        for (int i = 0; i < dropOrder.Count; i++)
        {
            Console.WriteLine($"    {i + 1}. {dropOrder[i].Schema}.{dropOrder[i].ViewName}");
        }
        Console.WriteLine();

        // 3. Generate SQL (PostgreSQL provider)
        Console.WriteLine("--- 3. Generated SQL ---");
        var provider = new PostgreSqlManagedViewProvider();

        foreach (var view in createOrder)
        {
            Console.WriteLine($"  -- {view.ViewName} --");
            Console.WriteLine($"  {provider.GenerateCreateSql(view)}");
            Console.WriteLine();

            foreach (string indexSql in provider.GenerateCreateIndexSql(view))
            {
                Console.WriteLine($"  {indexSql}");
            }
        }

        // 4. Hashing and change detection
        Console.WriteLine("--- 4. Change Detection ---");
        var hasher = new Sha256ViewHasher(managedViewOptions);
        var differ = new ManagedViewDiffer(hasher);

        Console.WriteLine("  Current hashes:");
        foreach (var view in views)
        {
            Console.WriteLine($"    {view.ViewName}: {hasher.ComputeHash(view.Sql)}");
        }
        Console.WriteLine();

        // Simulate first migration (no previous snapshot)
        var firstDiff = differ.ComputeDiff(views, previousSnapshot: null);
        Console.WriteLine($"  First migration: {firstDiff.Count} change(s)");
        foreach (var d in firstDiff)
        {
            Console.WriteLine($"    {d.DiffType}: {d.Definition.ViewName}");
        }
        Console.WriteLine();

        // Simulate second migration with matching snapshot (no changes)
        var snapshot = new ManagedViewSnapshot
        {
            Views = views.Select(v => new ManagedViewSnapshotEntry
            {
                ViewName = v.ViewName,
                Schema = v.Schema,
                Type = v.ViewType.ToString(),
                Hash = hasher.ComputeHash(v.Sql),
                Sql = v.Sql,
                DependsOn = v.DependsOn.ToList(),
                Indexes = v.Indexes.Select(i => new ManagedViewSnapshotIndexEntry
                {
                    Name = i.Name,
                    Columns = i.Columns
                }).ToList()
            }).ToList()
        };

        var noDiff = differ.ComputeDiff(views, snapshot);
        Console.WriteLine($"  Second migration (no changes): {noDiff.Count} change(s)");
        Console.WriteLine();

        // 5. Tracking table
        Console.WriteLine("--- 5. Tracking Table ---");
        var dummyOptions = new DbContextOptionsBuilder<DbContext>().UseInMemoryDatabase("dummy").Options;
        using var dummyContext = new DbContext(dummyOptions);
        var repo = new ManagedViewHistoryRepository(dummyContext, managedViewOptions);
        Console.WriteLine($"  {repo.GetCreateTableSql()}");
        Console.WriteLine();

        // 6. Snapshot system
        Console.WriteLine("--- 6. Snapshot ---");
        var snapshotStore = new JsonSnapshotStore();
        string tempDir = Path.Combine(Path.GetTempPath(), "managed_views_sample");
        snapshotStore.Save(snapshot, tempDir, "ECommerce");
        Console.WriteLine($"  Snapshot saved to: {Path.Combine(tempDir, "ECommerceManagedViewsSnapshot.json")}");

        var loaded = snapshotStore.Load(tempDir, "ECommerce");
        Console.WriteLine($"  Loaded snapshot: {loaded?.Views.Count} view(s), generated at {loaded?.GeneratedAt:u}");

        // Cleanup
        Directory.Delete(tempDir, recursive: true);

        Console.WriteLine();
        Console.WriteLine("=== Done ===");
    }
}
