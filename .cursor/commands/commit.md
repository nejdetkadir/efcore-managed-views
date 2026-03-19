# Git Commit

Analyze the current staged/unstaged changes and generate a conventional commit.

## Format

```
<type>(<scope>): <description>
```

## Types

| Type | When to use |
|------|-------------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `test` | Adding or updating tests |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `chore` | Build, CI, tooling changes |
| `perf` | Performance improvement |

## Scopes

- `core` -- EntityFrameworkCore.ManagedViews changes
- `postgresql` -- PostgreSQL provider changes
- `tests` -- Test project changes
- `docs` -- Documentation changes
- `ci` -- CI/CD workflow changes

## Steps

1. Run `git diff --staged` and `git diff` to see all changes
2. Determine the type and scope from the changed files
3. Write a concise description (imperative mood, no period)
4. Stage relevant files with `git add`
5. Commit with the generated message

## Examples

- `feat(core): add support for computed columns in view definitions`
- `fix(postgresql): handle null schema in materialized view refresh`
- `test(core): cover edge cases in topological sort cycle detection`
- `docs: update architecture diagram with new diffing pipeline`
