# Add Managed View

Scaffold a new managed view definition with all required files.

## Gather Information

Ask for:
1. **View name** (e.g., `vw_active_orders`)
2. **Schema** (default: `public`)
3. **Type**: `view` or `materialized`
4. **Dependencies**: other views it depends on (if any)
5. **Indexes**: for materialized views, which columns to index

## Create Files

### 1. SQL File

Create `Views/{schema}.{viewName}.sql` (or `Views/{viewName}.sql` for default schema):

```sql
-- @viewName: {viewName}
-- @schema: {schema}
-- @type: {type}
-- @dependsOn: {dependencies}
-- @indexes: {indexes}

SELECT ...
FROM ...
```

### 2. Entity Class

Create a corresponding entity class:

```csharp
/// <summary>
/// Read-only projection for the {viewName} managed view.
/// </summary>
[Keyless]
public sealed class {EntityName}
{
    // Properties matching the SELECT columns
}
```

### 3. Entity Configuration

In the DbContext's `OnModelCreating`:

```csharp
modelBuilder.Entity<{EntityName}>(b =>
{
    b.HasNoKey();
    b.ToManagedView("{viewName}");
});
```

### 4. Ensure Embedded Resource

Verify the `.csproj` has:
```xml
<EmbeddedResource Include="Views/**/*.sql" />
```
