# ANSI Color Code Detection and Terminal Capability Detection Research
## Cross-Platform CLI Tools Targeting .NET 10.0

**Research Date**: December 25, 2025  
**Context**: Enhancing a Markdown formatter for a C# static analysis CLI tool to include color-coded severity levels (red for Error, yellow for Warning, blue for Info). Colors should only appear when output is going to a terminal that supports them, not when redirected to files or non-color terminals.

---

## Table of Contents

1. [.NET Color Detection APIs](#net-color-detection-apis)
2. [Environment Variable Standards](#environment-variable-standards)
3. [Platform Compatibility](#platform-compatibility)
4. [ANSI Escape Code Reference](#ansi-escape-code-reference)
5. [Detection Algorithm](#detection-algorithm)
6. [Common CLI Color Flags](#common-cli-color-flags)
7. [CI/CD Environment Detection](#cicd-environment-detection)
8. [Recommended Implementation Strategy](#recommended-implementation-strategy)

---

## .NET Color Detection APIs

### Console.IsOutputRedirected & Console.IsErrorRedirected

**Available Since**: .NET Core 2.0+ (including .NET 6, 8, 10)  
**Namespace**: `System`  
**Assembly**: `System.Console.dll`

#### Purpose
Determine whether the standard output or error streams have been redirected from the console to a file or another process.

#### API Signatures

```csharp
// Returns true if output is redirected (e.g., to a file or pipe)
public static bool Console.IsOutputRedirected { get; }

// Returns true if error stream is redirected
public static bool Console.IsErrorRedirected { get; }

// Returns true if input stream is redirected
public static bool Console.IsInputRedirected { get; }
```

#### Usage Pattern

```csharp
// Basic terminal detection
bool isTerminal = !Console.IsOutputRedirected;

// Check if we're writing to an interactive terminal
if (!Console.IsOutputRedirected)
{
    // Safe to use ANSI color codes
    Console.Write("\x1b[31mError:\x1b[0m ");
}
else
{
    // Output is redirected - avoid colors
    Console.Write("Error: ");
}
```

#### Key Behaviors

- **Returns `true`** when output is redirected via:
  - Shell redirection: `app.exe > output.txt`
  - Pipes: `app.exe | less`
  - Process redirection: `ProcessStartInfo.RedirectStandardOutput = true`
  
- **Returns `false`** when:
  - Writing directly to a console/terminal window
  - No redirection is active

#### Limitations

- **Does NOT** detect if the terminal supports color (legacy consoles without ANSI support)
- **Does NOT** check for environment variables like `NO_COLOR` or `FORCE_COLOR`
- **Does NOT** distinguish between color-capable and non-color terminals

### Console Color Properties (Legacy API)

**.NET Framework/Core** provides `Console.ForegroundColor` and `Console.BackgroundColor` properties, but these:
- Use legacy Windows Console API (not cross-platform ANSI sequences)
- Not recommended for new applications targeting multiple platforms
- Microsoft's roadmap phases out classic Console APIs in favor of VT sequences

---

## Environment Variable Standards

### NO_COLOR Standard

**Source**: [https://no-color.org/](https://no-color.org/)  
**Status**: Informal standard proposed in 2017, widely adopted (300+ tools as of Dec 2025)

#### Specification

> **Command-line software which adds ANSI color to its output by default should check for a `NO_COLOR` environment variable that, when present and not an empty string (regardless of its value), prevents the addition of ANSI color.**

#### Implementation

```csharp
string? noColor = Environment.GetEnvironmentVariable("NO_COLOR");
bool disableColor = !string.IsNullOrEmpty(noColor);

if (disableColor)
{
    // User explicitly requested no color
    return false;
}
```

#### Key Points

- **Presence** (not value) matters: `NO_COLOR=1`, `NO_COLOR=true`, `NO_COLOR=anything` all disable color
- **Empty string is ignored**: `NO_COLOR=` does NOT disable color
- **Takes precedence** over auto-detection
- **User-level configuration** should override `NO_COLOR`
- **Does NOT disable** other styling (bold, underline, italic) - only color

#### Tools Supporting NO_COLOR

- .NET: NLog (5.4.0+), Spectre.Console (0.1.0+), System.Console (.NET 6+)
- Build Tools: npm (5.8.0+), Gradle, Maven, Rust (cargo)
- CLI Tools: ripgrep, fd, bat, exa/eza, git (via config), PowerShell 7.2+
- Test Frameworks: pytest (6.0.0+), jest, xUnit (via libraries)
- CI/CD: GitHub Actions, Azure Pipelines, Jenkins (automatic detection)

### FORCE_COLOR Standard

**Source**: [https://force-color.org/](https://force-color.org/)  
**Status**: Informal standard proposed in 2023, growing adoption

#### Specification

> **Command-line software which outputs colored text should check for a `FORCE_COLOR` environment variable. When this variable is present and not an empty string (regardless of its value), it should force the addition of ANSI color.**

#### Implementation

```csharp
string? forceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
bool forceColorEnabled = !string.IsNullOrEmpty(forceColor);

if (forceColorEnabled)
{
    // User explicitly requested color (overrides redirection)
    return true;
}
```

#### Use Cases

- Piping output through tools like `less -R` (which preserves ANSI codes)
- CI environments that support color but don't appear as TTYs
- Remote terminal sessions
- Debugging color output in logs

#### Precedence

`FORCE_COLOR` should **override** auto-detection (like `Console.IsOutputRedirected`), but `NO_COLOR` typically takes precedence over `FORCE_COLOR` (user's explicit "no" wins).

#### Tools Supporting FORCE_COLOR

- JavaScript: chalk (1.0.0+), supports-color (3.0.0+), Node.js, Deno
- Python: rich (12.6.0+), termcolor (2.1.0+), Python 3.13+
- .NET: Limited native support (libraries like Spectre.Console support it)
- Test Frameworks: jest, pytest (6.0.0+), Nox

### TERM Environment Variable

**Platform**: Unix/Linux/macOS (Windows Terminal also sets it)  
**Purpose**: Indicates terminal type/capabilities

#### Common Values

| Value | Meaning |
|-------|---------|
| `dumb` | No special terminal features (no color) |
| `xterm` | Standard xterm (supports 8 colors) |
| `xterm-256color` | xterm with 256-color support |
| `screen` | GNU Screen terminal multiplexer |
| `tmux` | tmux terminal multiplexer |
| (unset) | Not running in a terminal |

#### Detection Pattern

```csharp
string? term = Environment.GetEnvironmentVariable("TERM");

// Disable color if TERM is "dumb" or unset
if (string.IsNullOrEmpty(term) || term == "dumb")
{
    return false;
}
```

#### Limitations

- **Windows**: Not reliably set on Windows (except in Windows Terminal, WSL, Git Bash)
- **Not authoritative**: Presence doesn't guarantee color support
- **Should be combined** with other checks

### COLORTERM Environment Variable

**Platform**: Modern terminals (Linux, macOS, Windows Terminal)  
**Purpose**: Indicates advanced color support (24-bit true color)

#### Common Values

| Value | Meaning |
|-------|---------|
| `truecolor` | 24-bit RGB color support |
| `24bit` | Same as `truecolor` |
| (any value) | Color is supported |
| (unset) | May or may not support color |

#### Detection Pattern

```csharp
string? colorterm = Environment.GetEnvironmentVariable("COLORTERM");
bool supportsTrueColor = colorterm == "truecolor" || colorterm == "24bit";

// If COLORTERM is set at all, terminal likely supports color
bool supportsColor = !string.IsNullOrEmpty(colorterm);
```

---

## Platform Compatibility

### Windows 10+ ANSI Support

#### Historical Context

**Before Windows 10 (2015)**: Windows Console did not support ANSI escape sequences natively. Applications had to:
- Use Win32 Console APIs (`SetConsoleTextAttribute`, `SetConsoleCursorPosition`)
- Load `ANSI.SYS` driver (DOS/early Windows)
- Use third-party libraries

**Windows 10 Anniversary Update (1607, August 2016)**: Introduced ANSI/VT100 support via:
- **ENABLE_VIRTUAL_TERMINAL_PROCESSING** flag (output)
- **ENABLE_VIRTUAL_TERMINAL_INPUT** flag (input)
- ConPTY (Pseudoconsole) infrastructure (October 2018, Windows 10 1809)

#### Current Status (.NET 10.0 Era)

**.NET 6+ on Windows 10+**: ANSI sequences work automatically on:
- **Windows Terminal** (default terminal in Windows 11)
- **Windows Console Host** (conhost.exe) with VT processing enabled
- **PowerShell Core** (pwsh.exe)
- **WSL/WSL2** terminals
- **Third-party terminals**: ConEmu, Cmder, Terminus

**Legacy Console Mode**: Users can opt into legacy mode (no ANSI support) via console properties. Detection:

```csharp
// On Windows, check if VT processing is supported
// This is typically handled automatically by .NET 6+
// but can be verified via P/Invoke if needed
```

#### Key Differences from macOS/Linux

| Feature | Windows 10+ | macOS/Linux |
|---------|-------------|-------------|
| ANSI Support | Yes (since 2016) | Always |
| Requires Flag | ENABLE_VIRTUAL_TERMINAL_PROCESSING | No |
| ConPTY/PTY | ConPTY (2018+) | PTY (decades) |
| Default Terminal | Windows Terminal (Win11) / conhost (Win10) | Terminal.app / gnome-terminal / etc. |
| Legacy Mode | Optional fallback | N/A |

### macOS Terminal Support

**All modern macOS versions** (10.5+) support ANSI escape codes by default:
- **Terminal.app**: Full 256-color + true color support
- **iTerm2**: Advanced features, true color, custom escape sequences
- **Alacritty**, **Kitty**: GPU-accelerated, full true color

**Detection**: No special checks needed; assume ANSI support if `TERM` is set and not `dumb`.

### Linux Terminal Support

**Standard terminals** support ANSI:
- **gnome-terminal**, **konsole**, **xfce4-terminal**: Full color support
- **xterm**: Standard 8/16 colors (256-color with `xterm-256color`)
- **rxvt**, **urxvt**: Extended color support
- **tmux**, **screen**: Terminal multiplexers, 256-color capable

**Detection**: Check `TERM` and `COLORTERM` environment variables.

---

## ANSI Escape Code Reference

### Basic Structure

**ANSI Control Sequence Introducer (CSI)**: `ESC [ <parameters> <command>`
- In C# string literals: `"\x1b["` or `"\u001b["`
- Hex representation: `0x1B`

### SGR (Select Graphic Rendition) - Color Codes

**Format**: `ESC [ <n> m` where `<n>` is a numeric code

#### Foreground Colors (Text)

| Color | Standard (30-37) | Bright (90-97) | Severity Mapping |
|-------|------------------|----------------|------------------|
| **Red** | `31` | `91` | **Error** (recommended: `\x1b[91m` or `\x1b[31m`) |
| **Yellow** | `33` | `93` | **Warning** (recommended: `\x1b[93m` or `\x1b[33m`) |
| **Blue** | `34` | `94` | **Info** (recommended: `\x1b[94m` or `\x1b[36m` cyan) |
| **Cyan** | `36` | `96` | Alternative for Info |
| Black | `30` | `90` | |
| Green | `32` | `92` | Success messages |
| Magenta | `35` | `95` | |
| White | `37` | `97` | |

#### Background Colors

| Color | Standard (40-47) | Bright (100-107) |
|-------|------------------|------------------|
| Red | `41` | `101` |
| Yellow | `43` | `103` |
| Blue | `44` | `104` |
| (etc.) | ... | ... |

#### Reset Codes

| Code | Effect |
|------|--------|
| `0` | Reset all attributes |
| `39` | Reset foreground to default |
| `49` | Reset background to default |

#### Text Attributes

| Code | Effect | Reset |
|------|--------|-------|
| `1` | Bold / Bright | `22` |
| `4` | Underline | `24` |
| `7` | Negative (swap fg/bg) | `27` |

### Recommended Severity Color Mappings

```csharp
// ANSI escape codes for severity levels
public static class AnsiColors
{
    public const string Reset = "\x1b[0m";
    
    // Severity colors (bright variants for better visibility)
    public const string Error = "\x1b[91m";     // Bright Red
    public const string Warning = "\x1b[93m";   // Bright Yellow
    public const string Info = "\x1b[96m";      // Bright Cyan (more visible than blue)
    
    // Alternative: Standard colors
    public const string ErrorStd = "\x1b[31m";     // Red
    public const string WarningStd = "\x1b[33m";   // Yellow
    public const string InfoStd = "\x1b[34m";      // Blue
    
    // Success/hint colors
    public const string Success = "\x1b[92m";   // Bright Green
}

// Usage
Console.WriteLine($"{AnsiColors.Error}[ERROR]{AnsiColors.Reset} Something went wrong");
Console.WriteLine($"{AnsiColors.Warning}[WARNING]{AnsiColors.Reset} Deprecated method");
Console.WriteLine($"{AnsiColors.Info}[INFO]{AnsiColors.Reset} Processing complete");
```

### True Color (24-bit RGB)

**Format**: `ESC [ 38 ; 2 ; <r> ; <g> ; <b> m` (foreground)  
**Format**: `ESC [ 48 ; 2 ; <r> ; <g> ; <b> m` (background)

```csharp
// Example: Red error (RGB: 255, 0, 0)
string errorRgb = "\x1b[38;2;255;0;0m";

// Example: Orange warning (RGB: 255, 165, 0)
string warningRgb = "\x1b[38;2;255;165;0m";
```

**Compatibility**: Requires `COLORTERM=truecolor` or modern terminal (Windows Terminal, iTerm2, most Linux terminals post-2015).

---

## Detection Algorithm

### Decision Tree for Auto Color Mode

```
START: Should color be enabled?
│
├─ Is NO_COLOR environment variable set and non-empty?
│  ├─ YES → DISABLE color (user preference)
│  └─ NO → Continue
│
├─ Is FORCE_COLOR environment variable set and non-empty?
│  ├─ YES → ENABLE color (user override)
│  └─ NO → Continue
│
├─ Is output redirected? (Console.IsOutputRedirected)
│  ├─ YES → DISABLE color (writing to file/pipe)
│  └─ NO → Continue (writing to terminal)
│
├─ Is TERM environment variable set?
│  ├─ NO → DISABLE color (not a terminal)
│  ├─ YES, value is "dumb" → DISABLE color (dumb terminal)
│  └─ YES, other value → Continue
│
├─ Is this a CI environment? (detect CI_NAME, GITHUB_ACTIONS, etc.)
│  ├─ YES → Check CI-specific color support
│  │   ├─ GitHub Actions: ENABLE if FORCE_COLOR set or not redirected
│  │   ├─ Azure Pipelines: ENABLE if TF_BUILD and not redirected
│  │   └─ Jenkins: DISABLE by default (unless FORCE_COLOR)
│  └─ NO → Continue
│
└─ Default: ENABLE color (interactive terminal detected)
```

### C# Implementation Example

```csharp
public enum ColorMode
{
    Never,    // --color=never
    Auto,     // --color=auto (default)
    Always    // --color=always
}

public class ColorDetector
{
    public static bool ShouldUseColor(ColorMode mode, TextWriter output)
    {
        // Explicit user choice via --color flag
        if (mode == ColorMode.Never)
            return false;
        if (mode == ColorMode.Always)
            return true;
            
        // Auto-detection for ColorMode.Auto
        
        // 1. Check NO_COLOR (highest priority for disabling)
        string? noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (!string.IsNullOrEmpty(noColor))
            return false;
            
        // 2. Check FORCE_COLOR (overrides auto-detection)
        string? forceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
        if (!string.IsNullOrEmpty(forceColor))
            return true;
            
        // 3. Check if output is redirected
        if (output == Console.Out && Console.IsOutputRedirected)
            return false;
        if (output == Console.Error && Console.IsErrorRedirected)
            return false;
            
        // 4. Check TERM environment variable
        string? term = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(term) || term == "dumb")
            return false;
            
        // 5. Check CI environments
        if (IsRunningInCI())
        {
            return SupportsCIColor();
        }
            
        // 6. Default: assume terminal supports color
        return true;
    }
    
    private static bool IsRunningInCI()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
    }
    
    private static bool SupportsCIColor()
    {
        // GitHub Actions supports color
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            return true;
            
        // Azure Pipelines supports color
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
            return true;
            
        // Jenkins typically doesn't support color (unless using AnsiColor plugin)
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL")))
            return false;
            
        return true; // Default for unknown CI
    }
}
```

---

## Common CLI Color Flags

### Standard Patterns

Most CLI tools follow these conventions (borrowed from GNU tools like `ls`, `grep`, `git`):

| Flag Pattern | Values | Description |
|--------------|--------|-------------|
| `--color[=WHEN]` | `auto`, `always`, `never` | GNU-style (most common) |
| `--color` | (boolean flag) | Enable color unconditionally |
| `--no-color` | (boolean flag) | Disable color unconditionally |
| `-c`, `-C` | (short form) | Less common, conflicts with other flags |

### Recommended Approach for .NET CLI

```csharp
// Using System.CommandLine (recommended for .NET CLI apps)
var colorOption = new Option<ColorMode>(
    name: "--color",
    description: "When to use color output",
    getDefaultValue: () => ColorMode.Auto
);

colorOption.AddAlias("--colour"); // Support British spelling

var rootCommand = new RootCommand();
rootCommand.AddOption(colorOption);
```

### Examples from Popular Tools

**Git**:
```bash
git diff --color=always
git log --color=auto
git status --no-color
```

**Grep**:
```bash
grep --color=auto "pattern" file.txt
grep --color=never "pattern" file.txt
```

**Cargo (Rust)**:
```bash
cargo build --color=always
cargo test --color=auto
```

**NPM**:
```bash
npm install --color=false
npm test --color=true
```

---

## CI/CD Environment Detection

### Why CI Detection Matters

CI environments often:
- Run in non-interactive shells
- Have `Console.IsOutputRedirected == true` (logs redirected)
- May or may not support ANSI color codes
- Benefit from color (GitHub Actions, Azure Pipelines do)

### Environment Variables for CI Detection

| CI Platform | Environment Variables | Color Support |
|-------------|----------------------|---------------|
| **GitHub Actions** | `CI=true`, `GITHUB_ACTIONS=true` | ✅ Yes (renders in web UI) |
| **Azure Pipelines** | `TF_BUILD=True`, `AGENT_NAME` | ✅ Yes (renders in web UI) |
| **GitLab CI** | `GITLAB_CI=true`, `CI=true` | ✅ Yes |
| **Jenkins** | `JENKINS_URL`, `BUILD_NUMBER` | ❌ No (unless AnsiColor plugin) |
| **CircleCI** | `CIRCLECI=true`, `CI=true` | ✅ Yes |
| **Travis CI** | `TRAVIS=true`, `CI=true` | ✅ Yes |
| **AppVeyor** | `APPVEYOR=True`, `CI=True` | ❌ No (limited support) |

### Detection Implementation

```csharp
public static class CIEnvironment
{
    public static bool IsCI => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
        
    public static bool IsGitHubActions => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        
    public static bool IsAzurePipelines => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"));
        
    public static bool IsJenkins => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
        
    public static bool SupportsColor()
    {
        if (IsGitHubActions || IsAzurePipelines) return true;
        if (IsJenkins) return false; // Unless JENKINS_ANSI_COLOR=true
        
        // Default: assume CI supports color if it's not Jenkins
        return IsCI;
    }
}
```

### GitHub Actions Color Support

GitHub Actions **fully supports** ANSI color codes:
- Renders color in web UI logs
- Sets `CI=true` and `GITHUB_ACTIONS=true`
- Does NOT set `FORCE_COLOR` automatically
- **Recommendation**: Enable color when `GITHUB_ACTIONS=true`

### Azure Pipelines Color Support

Azure DevOps Pipelines **supports** ANSI color:
- Renders color in web UI
- Sets `TF_BUILD=True` and `AGENT_NAME`
- **Recommendation**: Enable color when `TF_BUILD=True`

---

## Recommended Implementation Strategy

### Precedence Order (Highest to Lowest)

1. **User-provided `--color` flag** (if specified)
   - `--color=never` → Disable
   - `--color=always` → Enable
   - `--color=auto` → Continue to next check

2. **`NO_COLOR` environment variable** (if non-empty)
   - Disable color (respect user's global preference)

3. **`FORCE_COLOR` environment variable** (if non-empty)
   - Enable color (override auto-detection)

4. **Output redirection check** (`Console.IsOutputRedirected`)
   - If redirected → Disable color
   - If not redirected → Continue

5. **Terminal type check** (`TERM` environment variable)
   - If `TERM` is unset or `"dumb"` → Disable color
   - Otherwise → Continue

6. **CI environment check** (if applicable)
   - GitHub Actions / Azure Pipelines → Enable
   - Jenkins → Disable
   - Other CI → Enable (conservative default)

7. **Default fallback**
   - Enable color (assume modern terminal)

### Fallback Behavior

```csharp
public class ColorConfig
{
    public ColorMode Mode { get; set; } = ColorMode.Auto;
    
    public bool IsColorEnabled(TextWriter output)
    {
        // Precedence 1: User flag
        if (Mode == ColorMode.Never) return false;
        if (Mode == ColorMode.Always) return true;
        
        // Precedence 2: NO_COLOR
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
            return false;
            
        // Precedence 3: FORCE_COLOR
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FORCE_COLOR")))
            return true;
            
        // Precedence 4: Redirection
        if (output == Console.Out && Console.IsOutputRedirected)
            return false;
            
        // Precedence 5: TERM check
        string? term = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(term) || term == "dumb")
            return false;
            
        // Precedence 6: CI detection
        if (CIEnvironment.IsCI)
            return CIEnvironment.SupportsColor();
            
        // Precedence 7: Default
        return true;
    }
}
```

### Edge Cases to Handle

#### PowerShell ISE vs PowerShell Core

- **PowerShell ISE**: Does NOT support ANSI codes (legacy)
  - Detection: Check `$PSVersionTable.PSEdition` (not accessible from .NET directly)
  - Workaround: Rely on `Console.IsOutputRedirected` (ISE typically redirects)
  
- **PowerShell Core (pwsh)**: Fully supports ANSI codes
  - Windows 10+: Full support
  - Cross-platform: Full support

**Recommendation**: Don't special-case; rely on redirection check.

#### Color Stripping for Logs

When users redirect to logs but want plain text:
```bash
# User runs this - expects no color in output.log
dotnet run scan --output markdown > output.log
```

This is handled automatically by `Console.IsOutputRedirected`.

If user wants color in logs (e.g., for `less -R`):
```bash
# User explicitly requests color
dotnet run scan --output markdown --color=always > output.log
less -R output.log
```

#### Windows Terminal vs Legacy Console

- **Windows Terminal**: Full ANSI support (automatic in .NET 6+)
- **Legacy Console (conhost.exe)**: ANSI support if `ENABLE_VIRTUAL_TERMINAL_PROCESSING` enabled (automatic in .NET 6+)
- **Legacy Console in "Legacy Mode"**: No ANSI support

**Recommendation**: .NET 6+ handles this automatically. No special checks needed.

---

## Summary of Best Practices

### ✅ DO

1. **Check `Console.IsOutputRedirected`** before emitting ANSI codes
2. **Respect `NO_COLOR`** environment variable (disable color if set)
3. **Support `FORCE_COLOR`** to override auto-detection
4. **Provide `--color=auto|always|never` flag** for explicit user control
5. **Use bright color variants** (90-97) for better visibility
6. **Always emit reset codes** (`\x1b[0m`) after colored text
7. **Test in CI environments** (GitHub Actions, Azure Pipelines)
8. **Support both stdout and stderr** color detection separately

### ❌ DON'T

1. **Don't assume color support** based on OS alone (check environment)
2. **Don't mix Windows Console APIs** (SetConsoleTextAttribute) with ANSI codes
3. **Don't forget reset codes** (causes color "leaking" to subsequent output)
4. **Don't emit ANSI codes** when `Console.IsOutputRedirected == true` (unless `FORCE_COLOR` is set)
5. **Don't use true color (24-bit RGB)** without checking `COLORTERM=truecolor`
6. **Don't hard-code severity colors** - make them configurable (accessibility)

---

## References

- [NO_COLOR Standard](https://no-color.org/) - Informal standard for disabling color
- [FORCE_COLOR Standard](https://force-color.org/) - Informal standard for forcing color
- [Microsoft Docs: Console.IsOutputRedirected](https://learn.microsoft.com/en-us/dotnet/api/system.console.isoutputredirected)
- [Microsoft Docs: Console Virtual Terminal Sequences](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences)
- [Microsoft Docs: Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/)
- [Microsoft Docs: Console Apps in .NET](https://learn.microsoft.com/en-us/dotnet/standard/building-console-apps)
- [Microsoft Docs: Console Log Formatting](https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter)
- [xterm Control Sequences](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html)
- [ECMA-48 Standard](https://ecma-international.org/publications-and-standards/standards/ecma-48/) - Control Functions for Coded Character Sets

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-12-25 | Research | Initial research compilation for .NET 10.0 CLI tool color detection |

---

**End of Research Document**
