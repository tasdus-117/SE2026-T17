# Contributing Guide

## Code of Conduct

Be respectful, inclusive, and collaborative. We're all learning.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/SE2026-T17.git`
3. Add upstream: `git remote add upstream https://github.com/tasdus-117/SE2026-T17.git`
4. Create feature branch: `git checkout -b feature/your-feature`

## Development Setup

See [DEVELOPMENT.md](./DEVELOPMENT.md) for detailed setup instructions.

## Making Changes

### Code Style

**C# (.NET)**
- PascalCase for class/method names
- camelCase for variables
- Add XML documentation comments
- Use async/await for I/O operations
- Max line length: 120 characters

**GDScript (Godot)**
- snake_case for functions/variables
- PascalCase for class names
- UPPER_CASE for constants
- Use type hints
- Max line length: 100 characters

### Writing Tests

Every feature must have tests:

**C# Test Example**
```csharp
[Fact]
public async Task CreatePlayer_WithValidUsername_ReturnsPlayer()
{
    // Arrange
    var playerService = new PlayerService(_context, _logger);
    
    // Act
    var result = await playerService.CreatePlayerAsync("testplayer");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("testplayer", result.Username);
}
```

**GDScript Test Example**
```gdscript
# Using GdUnit
extends GdUnitTestSuite

func test_player_takes_damage() -> void:
    var player = load("res://scenes/player.tscn").instantiate()
    player.health = 100
    
    player.take_damage(20)
    
    assert_eq(player.health, 80)
```

### Documentation

- Update README.md if feature is user-facing
- Add comments to complex logic
- Update ARCHITECTURE.md if system changes
- Update PROTOCOL.md if network messages change

### Commit Message Format

```
[CATEGORY] Brief description (50 chars max)

More detailed explanation if needed.
- Bullet points work well
- Explain why, not just what

Fixes #123
Related to #456

CATEGORY: Feature, Fix, Docs, Perf, Refactor, Test
```

Examples:
- `[Feature] Add matchmaking system`
- `[Fix] Reconnection timeout not working`
- `[Docs] Update deployment guide`
- `[Perf] Optimize hit validation query`

## Submitting Changes

### Before Pushing

1. Update from upstream: `git pull upstream develop`
2. Run tests: `dotnet test` (server) or test in Godot (client)
3. Verify code style: No console warnings/errors
4. Check documentation: Updated if needed

### Push and Create PR

```bash
git push origin feature/your-feature
```

### Pull Request Description

```markdown
## What
Brief description of what this PR does

## Why
Why is this change needed?

## How
How does it work?

## Testing
How to test this feature?

## Checklist
- [ ] Tests pass
- [ ] Code follows style guide
- [ ] Documentation updated
- [ ] No breaking changes
- [ ] Performance impact: None / Improvements X%

Fixes #123
```

## Code Review Process

Reviewers will check:

1. **Correctness:** Does it work as intended?
2. **Style:** Does it follow guidelines?
3. **Performance:** Is it efficient?
4. **Documentation:** Is it documented?
5. **Testing:** Are there adequate tests?
6. **Security:** Any vulnerabilities?

**Addressing Feedback:**
- Respond to comments
- Make requested changes
- Don't force-push after review starts (update commits instead)
- React with ✓ when addressed

## Common Contributions

### Bug Report
```markdown
## Description
What happened?

## Steps to Reproduce
1. ...
2. ...
3. ...

## Expected
What should happen?

## Actual
What actually happened?

## Environment
- OS: 
- .NET version:
- Godot version:

## Logs
```
error output here
```
```

### Feature Request
```markdown
## Description
What feature would you like?

## Motivation
Why is this needed?

## Proposed Solution
How could it work?

## Alternatives
Other approaches?

## Examples
Any reference implementations?
```

## Review Checklist for Maintainers

Before merging:

- [ ] All CI checks pass
- [ ] At least 2 approvals
- [ ] No conflicts with `develop`
- [ ] Commits are squashed if needed
- [ ] PR title follows format
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No performance regression
- [ ] Security review complete

## Release Process

1. Create release branch from `develop`
2. Update version numbers
3. Update CHANGELOG.md
4. Create pull request to `main`
5. Tag release: `git tag -a v1.0.0 -m "Release 1.0.0"`
6. Create GitHub release with notes
7. Merge back to `develop`

## Conflict Resolution

If merge conflicts occur:

```bash
# Get latest main
git fetch upstream
git rebase upstream/develop

# Resolve conflicts in editor
git add .
git rebase --continue
git push origin feature/your-feature -f
```

## Questions?

- Open issue with `[question]` label
- Ask in discussions
- Contact team lead

## Resources

- [Godot Docs](https://docs.godotengine.org)
- [.NET Docs](https://docs.microsoft.com/dotnet)
- [Git Docs](https://git-scm.com/doc)
- [Protocol Docs](./docs/PROTOCOL.md)
- [Architecture Docs](./docs/ARCHITECTURE.md)

## Attribution

Your contribution will be:
- Credited in CHANGELOG.md
- Listed in GitHub contributors
- Acknowledged in release notes

Thank you for contributing! 🎉
