using System.Diagnostics.CodeAnalysis;

// Polyfill files intentionally contain multiple types for compatibility
[assembly:
    SuppressMessage("Naming", "MA0048:File name must match type name",
        Justification = "Polyfills contain multiple compatibility types", Scope = "type",
        Target = "~T:System.ArgumentExceptionPolyfills")]
[assembly:
    SuppressMessage("Naming", "MA0048:File name must match type name",
        Justification = "Polyfills contain multiple compatibility types", Scope = "type",
        Target = "~T:System.ArgumentNullExceptionPolyfills")]
[assembly:
    SuppressMessage("Naming", "MA0048:File name must match type name",
        Justification = "Polyfills contain multiple compatibility types", Scope = "type",
        Target = "~T:System.EnumPolyfills")]
[assembly:
    SuppressMessage("Naming", "MA0048:File name must match type name",
        Justification = "Polyfills contain multiple compatibility types", Scope = "type",
        Target = "~T:System.MathPolyfills")]
[assembly:
    SuppressMessage("Naming", "MA0048:File name must match type name",
        Justification = "RuleExecutionError is the corrected name, file kept as RuleException for compatibility",
        Scope = "type", Target = "~T:Lintelligent.AnalyzerEngine.Analysis.RuleExecutionError")]

// Polyfill compatibility: Caller info attributes intentionally passed through
[assembly:
    SuppressMessage("Design", "S3236:Caller information arguments should not be provided explicitly",
        Justification = "Polyfill forwards to framework implementation in net10.0", Scope = "member",
        Target = "~M:System.ArgumentExceptionPolyfills.ThrowIfNullOrWhiteSpace(System.String,System.String)")]
[assembly:
    SuppressMessage("Design", "S3236:Caller information arguments should not be provided explicitly",
        Justification = "Polyfill forwards to framework implementation in net10.0", Scope = "member",
        Target = "~M:System.ArgumentNullExceptionPolyfills.ThrowIfNull(System.Object,System.String)")]
