# Create Pull Request

Generate a comprehensive pull request using GitHub CLI.

## Steps

1. Check current branch and ensure it's not `main`
2. Run `git log main..HEAD --oneline` to see all commits
3. Run `git diff main...HEAD --stat` to see changed files
4. Run `dotnet test` to verify all tests pass
5. Push the branch: `git push -u origin HEAD`
6. Create the PR with `gh pr create`

## PR Template

```markdown
## Summary

[2-3 bullet points describing what changed and why]

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Test Plan

- [ ] All existing tests pass (`dotnet test`)
- [ ] Added tests for new code paths
- [ ] Coverage remains at 100%
- [ ] No new build warnings

## Related Issues

Fixes #
```

## Title Convention

Use conventional commit style: `feat(scope): description` or `fix(scope): description`
