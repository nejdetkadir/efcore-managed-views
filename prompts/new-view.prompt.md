# Add a New Managed View

Add a new SQL view definition to the project.

## For SQL File Approach

1. Create a .sql file in the Views/ directory
2. Add directives: @viewName, @schema, @type
3. Add @dependsOn if it depends on other views
4. Add @indexes for materialized views
5. Mark as embedded resource in .csproj
6. Create a matching entity class with [HasNoKey]
7. Map with modelBuilder.Entity<T>().ToManagedView("name")

## For Fluent API Approach

1. In OnModelCreating, call modelBuilder.HasManagedView("name", v => v.AsSql("..."))
2. Set schema with .InSchema("schema")
3. Set type with .AsMaterialized() if needed
4. Add dependencies with .DependsOn("other_view")
5. Create a matching entity class with [HasNoKey]
6. Map with modelBuilder.Entity<T>().ToManagedView("name")
