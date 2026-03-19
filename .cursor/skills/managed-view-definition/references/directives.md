# SQL File Directive Reference

## Available Directives

| Directive | Format | Required | Default |
|-----------|--------|----------|---------|
| `@viewName` | `-- @viewName: name` | No | Inferred from filename |
| `@schema` | `-- @schema: schema_name` | No | Inferred from filename or `public` |
| `@type` | `-- @type: view\|materialized` | No | `view` |
| `@dependsOn` | `-- @dependsOn: view1, view2` | No | None |
| `@indexes` | `-- @indexes: idx(col1); idx2(col2 DESC)` | No | None |

## File Naming Conventions

| Filename | Inferred Name | Inferred Schema |
|----------|---------------|-----------------|
| `vw_products.sql` | `vw_products` | default (`public`) |
| `catalog.vw_products.sql` | `vw_products` | `catalog` |
| `sales.mv_daily_stats.sql` | `mv_daily_stats` | `sales` |

Directives in the file override inferred values.

## Index Format

```
-- @indexes: idx_cat_stats(category_name); idx_price(price DESC)
```

- Semicolon-separated for multiple indexes
- Parentheses contain column definitions
- Supports `ASC`/`DESC` modifiers
- Only applicable to materialized views

## Parsing

- Directives must be SQL single-line comments (`-- @directive: value`)
- Directives must appear before the first SQL statement
- Whitespace around values is trimmed
- Parser: `SqlFileMetadataParser` in `Discovery/`
