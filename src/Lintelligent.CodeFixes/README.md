# Lintelligent Code Fixes

This package provides code fix providers for Lintelligent analyzers, enabling one-click refactorings to functional programming patterns.

## Features

### LNT200: Convert to Option\<T\>

Transforms nullable return types to `Option<T>` from LanguageExt.

**What it does:**
- Changes method signature: `string?` → `Option<string>`
- Transforms null returns: `return null` → `Option<string>.None`
- Keeps value returns as-is (uses implicit conversion: `return value` → `Option<string>`)
- Adds `using LanguageExt;` if not present

**Example:**

```csharp
// Before
private static SyntaxTree? ParseFile(string path)
{
    if (!File.Exists(path))
        return null;
    return CSharpSyntaxTree.ParseText(File.ReadAllText(path));
}

// After (one click)
private static Option<SyntaxTree> ParseFile(string path)
{
    if (!File.Exists(path))
        return Option<SyntaxTree>.None;
    return CSharpSyntaxTree.ParseText(File.ReadAllText(path));
}
```

**Known Limitations:**

1. **Call sites not updated automatically** - After applying the fix, you'll need to update code that calls this method. Common patterns:
   ```csharp
   // Old pattern (will show compiler error after fix):
   SyntaxTree? tree = ParseFile(path);
   if (tree != null) yield return tree;
   
   // Fix manually to:
   foreach (var tree in ParseFile(path).ToSeq())
       yield return tree;
   ```

2. **Single file scope** - Only fixes the method declaration, not usages in other files

3. **Pattern matching not added** - Doesn't convert `if (x != null)` to `x.Match(...)` automatically

These limitations are by design to keep transformations predictable and safe. For complex refactorings, consider using "Fix All in Solution" after reviewing changes.

## Installation

This package is intended to be used alongside `Lintelligent.Analyzers`:

```xml
<ItemGroup>
  <PackageReference Include="Lintelligent.Analyzers" Version="1.2.0" />
  <PackageReference Include="Lintelligent.CodeFixes" Version="1.0.0-preview" />
</ItemGroup>
```

## Roadmap

Future code fix providers:
- **LNT201**: Convert try/catch to `Either<Error, T>`
- **LNT202**: Convert sequential validations to `Validation<Error, T>`
- **Call site fixer**: Automatically update method calls to use `.ToSeq()`, `.Match()`, etc.

## License

This package is source-available but not open source. Commercial or production use requires a valid Lintelligent Pro license key.

- Permitted: View, study, and modify for evaluation or contribution
- Prohibited: Commercial use without a license, redistribution, or license circumvention

See [LICENSE-PRO.md](../../LICENSE-PRO.md) for full terms.
