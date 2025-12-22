using Lintelligent.AnalyzerEngine.Results;

namespace Lintelligent.AnalyzerEngine.Analysis;

public class AnalyzerEngine(AnalyzerManager manager)
{
    public IEnumerable<DiagnosticResult> Analyze(string projectPath)
    {
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(source, path: file);

            foreach (var diagnostic in manager.Analyze(tree))
            {
                yield return diagnostic;
            }
        }
    }
}
