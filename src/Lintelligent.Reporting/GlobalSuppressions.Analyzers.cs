using System.Diagnostics.CodeAnalysis;

// Reporting project suppressions
[assembly:
    SuppressMessage("Design", "MA0048:File name must match type name",
        Justification = "Multiple related models in single file", Scope = "type",
        Target = "~T:Lintelligent.Reporting.Formatters.Models.SummaryModel")]
[assembly:
    SuppressMessage("Design", "MA0048:File name must match type name",
        Justification = "Multiple related models in single file", Scope = "type",
        Target = "~T:Lintelligent.Reporting.Formatters.Models.ViolationModel")]

// Collection types in DTOs - concrete types needed for JSON serialization
[assembly:
    SuppressMessage("Design", "MA0016:Prefer using collection abstraction instead of implementation",
        Justification = "JSON serialization requires concrete List/Dictionary types", Scope = "member",
        Target = "~P:Lintelligent.Reporting.Formatters.Models.JsonOutputModel.Violations")]
[assembly:
    SuppressMessage("Design", "MA0016:Prefer using collection abstraction instead of implementation",
        Justification = "JSON serialization requires concrete Dictionary type", Scope = "member",
        Target = "~P:Lintelligent.Reporting.Formatters.Models.SummaryModel.BySeverity")]

// String operations in report generation - culture not relevant for markdown/json
[assembly:
    SuppressMessage("Performance",
        "MA0002:Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        Justification = "Culture-invariant behavior acceptable for report generation", Scope = "member",
        Target =
            "~M:Lintelligent.Reporting.ReportGenerator.GenerateMarkdownGroupedByCategory(System.Collections.Generic.IEnumerable{Lintelligent.AnalyzerEngine.Results.DiagnosticResult})~System.String")]
[assembly:
    SuppressMessage("Performance",
        "MA0002:Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        Justification = "Culture-invariant behavior acceptable for JSON grouping", Scope = "member",
        Target =
            "~M:Lintelligent.Reporting.Formatters.JsonFormatter.GenerateReport(System.Collections.Generic.IEnumerable{Lintelligent.AnalyzerEngine.Results.DiagnosticResult})~System.String")]

// String concatenation in markdown generation - acceptable for small outputs
[assembly:
    SuppressMessage("Major Code Smell", "S1643:Use a StringBuilder instead",
        Justification = "Markdown generation has limited output size", Scope = "member",
        Target =
            "~M:Lintelligent.Reporting.ReportGenerator.GenerateMarkdownGroupedByCategory(System.Collections.Generic.IEnumerable{Lintelligent.AnalyzerEngine.Results.DiagnosticResult})~System.String")]

// OutputConfiguration.Validate - property name in exception is correct
[assembly:
    SuppressMessage("Major Code Smell", "S3928:Parameter names used into ArgumentException",
        Justification = "Format is a property name, nameof is correct", Scope = "member",
        Target = "~M:Lintelligent.Reporting.Formatters.OutputConfiguration.Validate")]
[assembly:
    SuppressMessage("Design", "MA0015:ArgumentException parameter name",
        Justification = "Format is a property name, nameof is correct", Scope = "member",
        Target = "~M:Lintelligent.Reporting.Formatters.OutputConfiguration.Validate")]

// JsonFormatter.SelectMany - culture-invariant grouping acceptable
[assembly:
    SuppressMessage("Performance",
        "MA0002:Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        Justification = "LINQ projection, not string comparison", Scope = "member",
        Target =
            "~M:Lintelligent.Reporting.Formatters.JsonFormatter.GenerateReport(System.Collections.Generic.IEnumerable{Lintelligent.AnalyzerEngine.Results.DiagnosticResult})~System.String")]
