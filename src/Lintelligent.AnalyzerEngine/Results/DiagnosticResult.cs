namespace Lintelligent.AnalyzerEngine.Results;

public record DiagnosticResult(string FilePath, string RuleId, string Message, int LineNumber);
