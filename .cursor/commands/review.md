# Code Review

Review the current file or selected code against the EntityFrameworkCore.ManagedViews project standards.

## Checklist

1. **Naming**: Does it follow conventions? (_camelCase fields, IPrefix interfaces, PascalCase types)
2. **Sealed**: Is the class sealed? If not, is inheritance explicitly needed?
3. **Null safety**: Are parameters validated with `ThrowIfNull()` / `ThrowIfNullOrWhiteSpace()`?
4. **XML docs**: Do all public members have `<summary>` documentation?
5. **Error handling**: Are custom `ManagedView*Exception` types used instead of raw exceptions?
6. **DI registration**: If a new service, is it registered in `ApplyServices`?
7. **Analyzers**: Will this pass with `TreatWarningsAsErrors: true`?
8. **Test coverage**: Is there a corresponding test covering every code path?

## Output Format

For each issue found:
- **Rule**: Which standard is violated
- **Location**: File and line
- **Suggestion**: How to fix it
- **Severity**: ERROR | WARNING | INFO

End with a summary: APPROVE, REQUEST_CHANGES, or COMMENT.
