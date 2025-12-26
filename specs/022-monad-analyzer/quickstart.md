# Quick Start: Language-Ext Monad Detection

**Feature**: 022-monad-analyzer  
**Phase**: 1 (Design)  
**Date**: 2024-12-26

## Overview

The Language-Ext Monad Detection Analyzer helps you identify opportunities to replace imperative error handling patterns with functional monads from the [language-ext](https://github.com/louthy/language-ext) library.

This analyzer is **opt-in** and requires configuration via `.editorconfig`.

---

## Installation

### 1. Add language-ext NuGet Package

```bash
dotnet add package LanguageExt.Core
```

### 2. Enable Monad Detection in .editorconfig

Create or modify `.editorconfig` at your solution root:

```ini
root = true

[*.cs]
# Enable language-ext monad detection (default: false)
language_ext_monad_detection = true

# Optional: Set minimum complexity threshold (default: 3)
language_ext_min_complexity = 3

# Optional: Enable specific monad types (default: all)
language_ext_enabled_monads = option, either, validation
```

### 3. Build Project

```bash
dotnet build
```

You should now see monad suggestions in your IDE (e.g., Visual Studio, Rider, VS Code).

---

## Examples

### Option<T> - Nullable Return Types

**Before** (LNT200 diagnostic):
```csharp
public string? FindUserName(int userId)
{
    if (userId < 0)
        return null;

    var user = _repository.GetById(userId);
    if (user == null)
        return null;

    return user.Name;
}

// Caller must remember to check for null
var name = FindUserName(123);
if (name != null)
{
    Console.WriteLine(name);
}
```

**After** (using Option<T>):
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

public Option<string> FindUserName(int userId)
{
    if (userId < 0)
        return None;

    var user = _repository.GetById(userId);
    if (user == null)
        return None;

    return Some(user.Name);
}

// Compiler forces explicit handling of missing value
FindUserName(123).Match(
    Some: name => Console.WriteLine(name),
    None: () => Console.WriteLine("User not found")
);
```

**Benefits**:
- ✅ Eliminates null reference exceptions at compile time
- ✅ Forces caller to handle missing value case
- ✅ Self-documenting: signature shows value may be absent

---

### Either<L, R> - Error Handling

**Before** (LNT201 diagnostic):
```csharp
public int ParseUserId(string input)
{
    try
    {
        var id = int.Parse(input);
        if (id < 0)
            throw new ArgumentException("User ID must be positive");
        
        return id;
    }
    catch (Exception ex)
    {
        return -1; // Magic error value
    }
}

// Caller doesn't know why it failed
var userId = ParseUserId("abc");
if (userId == -1)
{
    // What went wrong? Parse error? Negative value?
}
```

**After** (using Either<L, R>):
```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public Either<Error, int> ParseUserId(string input)
{
    try
    {
        var id = int.Parse(input);
        if (id < 0)
            return Left(Error.New("User ID must be positive"));
        
        return Right(id);
    }
    catch (Exception ex)
    {
        return Left(Error.New(ex));
    }
}

// Caller gets full error context
ParseUserId("abc").Match(
    Right: id => Console.WriteLine($"User ID: {id}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

**Benefits**:
- ✅ Makes error handling explicit in method signature
- ✅ Preserves error context for caller
- ✅ Enables Railway-Oriented Programming

---

### Validation<T> - Accumulating Errors

**Before** (LNT202 diagnostic):
```csharp
public Result<User> ValidateUser(string name, string email, int age)
{
    // Fails fast - user only sees first error
    if (string.IsNullOrEmpty(name))
        return Error("Name is required");
    
    if (!email.Contains("@"))
        return Error("Invalid email format");
    
    if (age < 18)
        return Error("Must be 18 or older");
    
    return Success(new User(name, email, age));
}

// User submits form, sees "Name is required"
// Fixes name, resubmits, now sees "Invalid email format"
// Frustrating UX - multiple round trips!
```

**After** (using Validation<T>):
```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public Validation<Error, User> ValidateUser(string name, string email, int age)
{
    var validateName = string.IsNullOrEmpty(name)
        ? Fail<Error, string>(Error.New("Name is required"))
        : Success<Error, string>(name);

    var validateEmail = !email.Contains("@")
        ? Fail<Error, string>(Error.New("Invalid email format"))
        : Success<Error, string>(email);

    var validateAge = age < 18
        ? Fail<Error, int>(Error.New("Must be 18 or older"))
        : Success<Error, int>(age);

    return (validateName, validateEmail, validateAge)
        .Apply((n, e, a) => new User(n, e, a));
}

// User sees ALL validation errors in one request
ValidateUser("", "invalid", 16).Match(
    Succ: user => Console.WriteLine("User created"),
    Fail: errors => errors.Iter(e => Console.WriteLine($"- {e.Message}"))
);
// Output:
// - Name is required
// - Invalid email format
// - Must be 18 or older
```

**Benefits**:
- ✅ Accumulates all errors before returning
- ✅ Better UX: user sees complete feedback
- ✅ Parallel validation (independent checks)

---

### Try<T> - Exception-Prone Operations

**Before** (LNT203 diagnostic):
```csharp
public Config LoadConfig(string path)
{
    // May throw FileNotFoundException, JsonException, etc.
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<Config>(json);
}

// Caller must wrap in try/catch
try
{
    var config = LoadConfig("appsettings.json");
    Console.WriteLine(config.Version);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load config: {ex.Message}");
}
```

**After** (using Try<T>):
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

public Try<Config> LoadConfig(string path) => () =>
{
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<Config>(json);
};

// No try/catch boilerplate needed
LoadConfig("appsettings.json").Match(
    Succ: config => Console.WriteLine(config.Version),
    Fail: ex => Console.WriteLine($"Failed to load config: {ex.Message}")
);
```

**Benefits**:
- ✅ Converts exceptions to values for functional composition
- ✅ Lazy evaluation: exception only thrown when matched
- ✅ Eliminates try/catch boilerplate

---

## Configuration Reference

### language_ext_monad_detection

**Type**: `bool`  
**Default**: `false`  
**Description**: Enable/disable monad detection globally.

```ini
[*.cs]
language_ext_monad_detection = true
```

---

### language_ext_min_complexity

**Type**: `int`  
**Default**: `3`  
**Description**: Minimum complexity threshold for suggestions.

- **Option<T>**: Minimum null checks or null returns
- **Validation<T>**: Minimum sequential validations

```ini
[*.cs]
language_ext_min_complexity = 5  # Only flag methods with 5+ null checks
```

---

### language_ext_enabled_monads

**Type**: `string` (comma-separated)  
**Default**: `option, either, validation, try`  
**Description**: Specific monad types to detect.

```ini
[*.cs]
# Only detect Option and Either patterns
language_ext_enabled_monads = option, either
```

---

### Severity Overrides

Control severity per diagnostic:

```ini
[*.cs]
# Show as suggestions (not info)
dotnet_diagnostic.LNT200.severity = suggestion
dotnet_diagnostic.LNT201.severity = suggestion
dotnet_diagnostic.LNT202.severity = suggestion

# Disable Try<T> detection
dotnet_diagnostic.LNT203.severity = none
```

---

## Diagnostic IDs

| ID | Description | Default Severity |
|----|-------------|------------------|
| LNT200 | Nullable → Option<T> | Info |
| LNT201 | Try/Catch → Either<L, R> | Info |
| LNT202 | Sequential Validation → Validation<T> | Info |
| LNT203 | Exception Flow → Try<T> | Info |

---

## Frequently Asked Questions

### Q: Does this analyzer change my code automatically?

**A**: No. This analyzer provides **suggestions only** (Info severity). Code fixes (auto-refactoring) are planned for a future release.

### Q: Will this work if I don't have language-ext installed?

**A**: The analyzer will only suggest monad patterns if `LanguageExt.Core` is referenced in your project. If the package is not installed, no diagnostics are reported.

### Q: Can I use this with async/await?

**A**: Yes! Option<T> works with `Task<Option<T>>`, and Either/Try support async composition via `MapAsync`, `BindAsync`.

### Q: Does this affect build performance?

**A**: Minimal impact (<10% overhead). The analyzer uses semantic analysis only when `language_ext_monad_detection = true`.

### Q: Can I disable this for specific files?

**A**: Yes, use .editorconfig file-specific rules:

```ini
# Disable for generated files
[**/Generated/*.cs]
dotnet_diagnostic.LNT200.severity = none
dotnet_diagnostic.LNT201.severity = none
dotnet_diagnostic.LNT202.severity = none
dotnet_diagnostic.LNT203.severity = none
```

---

## Additional Resources

- [language-ext GitHub](https://github.com/louthy/language-ext)
- [language-ext Wiki](https://github.com/louthy/language-ext/wiki)
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Functional C# with language-ext](https://github.com/louthy/language-ext#introduction)

---

**Status**: Quick start guide complete. Provides installation, examples, and configuration reference.
