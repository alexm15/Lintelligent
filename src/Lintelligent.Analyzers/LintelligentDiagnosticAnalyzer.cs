using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Lintelligent.AnalyzerEngine.Abstractions;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.Analyzers.Adapters;
using Lintelligent.Analyzers.Metadata;

namespace Lintelligent.Analyzers;

/// <summary>
/// Main Roslyn diagnostic analyzer that wraps all Lintelligent IAnalyzerRule implementations.
/// </summary>
/// <remarks>
/// This analyzer:
/// - Discovers all IAnalyzerRule implementations via reflection at initialization
/// - Creates DiagnosticDescriptor for each rule
/// - Executes all rules on each syntax tree during compilation
/// - Supports EditorConfig severity overrides (dotnet_diagnostic.{ruleId}.severity)
/// - Skips generated code automatically
/// - Reports internal errors as LNT999 without crashing
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LintelligentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly IAnalyzerRule[] Rules = DiscoverRules();
#if !NETSTANDARD2_0
    private static readonly IWorkspaceAnalyzer[] WorkspaceAnalyzers = DiscoverWorkspaceAnalyzers();
    private static readonly DiagnosticDescriptor[] WorkspaceDescriptors = CreateWorkspaceDescriptors(WorkspaceAnalyzers);
    private static readonly Dictionary<string, DiagnosticDescriptor> WorkspaceDescriptorMap = WorkspaceDescriptors.ToDictionary(d => d.Id, StringComparer.Ordinal);
#endif
    private static readonly DiagnosticDescriptor[] Descriptors = CreateDescriptors(Rules);
    private static readonly Dictionary<string, DiagnosticDescriptor> DescriptorMap = Descriptors.ToDictionary(d => d.Id, StringComparer.Ordinal);
    
    // Internal error descriptor for LNT999
    private static readonly DiagnosticDescriptor InternalErrorDescriptor = new(
        id: "LNT999",
        title: "Internal Analyzer Error",
        messageFormat: "Analyzer error in {0}: {1}",
        category: "InternalError",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An internal error occurred while running the analyzer. This may indicate a bug in the analyzer.");

    /// <summary>
    /// Gets the set of diagnostic descriptors supported by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
#if !NETSTANDARD2_0
        => ImmutableArray.Create(Descriptors.Concat(WorkspaceDescriptors).Append(InternalErrorDescriptor).ToArray());
#else
        => ImmutableArray.Create(Descriptors.Append(InternalErrorDescriptor).ToArray());
#endif

    /// <summary>
    /// Initializes the analyzer and registers callbacks.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);  // Skip generated code
        context.EnableConcurrentExecution();  // Thread-safe parallel execution
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
#if !NETSTANDARD2_0
        context.RegisterCompilationStartAction(AnalyzeCompilation);  // Workspace-level analysis
#endif
    }

    /// <summary>
    /// Analyzes a syntax tree by executing all rules.
    /// </summary>
    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var configOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);

        foreach (var rule in Rules)
        {
            try
            {
                // Check EditorConfig for severity override
                if (configOptions.TryGetValue($"dotnet_diagnostic.{rule.Id}.severity", out var severity) && 
                    SeverityMapper.IsSuppressed(severity))
                {
                    continue;  // Suppressed via EditorConfig
                }

                // Execute rule analysis
                var results = rule.Analyze(context.Tree);
                var descriptor = DescriptorMap[rule.Id];

                foreach (var result in results)
                {
                    var diagnostic = DiagnosticConverter.Convert(result, context.Tree, descriptor);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash analyzer
                ReportInternalError(context, rule.Id, ex.Message);
            }
        }
    }

    /// <summary>
    /// Discovers all IAnalyzerRule implementations in the AnalyzerEngine assembly.
    /// </summary>
    private static IAnalyzerRule[] DiscoverRules()
    {
        var ruleInterface = typeof(IAnalyzerRule);
        var assembly = ruleInterface.Assembly;

        var ruleTypes = assembly.GetTypes()
            .Where(t => ruleInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        var rules = new List<IAnalyzerRule>();
        foreach (var ruleType in ruleTypes)
        {
            try
            {
#if NETSTANDARD2_0
                var rule = (IAnalyzerRule)Activator.CreateInstance(ruleType)!;
#else
                var rule = (IAnalyzerRule)Activator.CreateInstance(ruleType)!;
#endif
                rules.Add(rule);
            }
            catch (Exception ex)
            {
                // Log discovery failure (MSBuild diagnostic output)
                System.Diagnostics.Debug.WriteLine($"[Lintelligent] Failed to load rule {ruleType.Name}: {ex.Message}");
            }
        }

        if (rules.Count == 0)
        {
            // Critical error - no rules found (misconfiguration)
            throw new InvalidOperationException(
                $"No IAnalyzerRule implementations found in assembly {assembly.GetName().Name}. " +
                "Ensure Lintelligent.AnalyzerEngine.dll is packaged correctly.");
        }

        return rules.ToArray();
    }

    /// <summary>
    /// Creates DiagnosticDescriptor for each rule.
    /// </summary>
    private static DiagnosticDescriptor[] CreateDescriptors(IAnalyzerRule[] rules)
    {
        return rules.Select(RuleDescriptorFactory.Create).ToArray();
    }

#if !NETSTANDARD2_0
    /// <summary>
    /// Discovers all IWorkspaceAnalyzer implementations in the AnalyzerEngine assembly.
    /// </summary>
    private static IWorkspaceAnalyzer[] DiscoverWorkspaceAnalyzers()
    {
        var analyzerInterface = typeof(IWorkspaceAnalyzer);
        var assembly = analyzerInterface.Assembly;

        var analyzerTypes = assembly.GetTypes()
            .Where(t => analyzerInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        var analyzers = new List<IWorkspaceAnalyzer>();
        foreach (var analyzerType in analyzerTypes)
        {
            try
            {
#if NETSTANDARD2_0
                var analyzer = (IWorkspaceAnalyzer)Activator.CreateInstance(analyzerType)!;
#else
                var analyzer = (IWorkspaceAnalyzer)Activator.CreateInstance(analyzerType)!;
#endif
                analyzers.Add(analyzer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lintelligent] Failed to load workspace analyzer {analyzerType.Name}: {ex.Message}");
            }
        }

        return analyzers.ToArray();
    }

    /// <summary>
    /// Creates DiagnosticDescriptor for each workspace analyzer.
    /// </summary>
    private static DiagnosticDescriptor[] CreateWorkspaceDescriptors(IWorkspaceAnalyzer[] analyzers)
    {
        return analyzers.Select(a => new DiagnosticDescriptor(
            id: a.Id,
            title: a.Description,
            messageFormat: "{0}",
            category: a.Category,
            defaultSeverity: a.Severity == Lintelligent.AnalyzerEngine.Results.Severity.Error 
                ? DiagnosticSeverity.Error 
                : DiagnosticSeverity.Warning,
            isEnabledByDefault: true)).ToArray();
    }

    /// <summary>
    /// Analyzes entire compilation (all syntax trees) for workspace-level rules.
    /// </summary>
    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        // Run at compilation end to ensure all trees are available
        context.RegisterCompilationEndAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            var trees = compilation.SyntaxTrees.ToList();
            
            // Create minimal solution and project for workspace context
            var project = new Lintelligent.AnalyzerEngine.ProjectModel.Project(
                filePath: string.Empty,
                name: compilation.AssemblyName ?? "Unknown",
                rootNamespace: compilation.AssemblyName ?? string.Empty,
                targetFramework: string.Empty,
                outputType: string.Empty,
                assemblyName: compilation.AssemblyName ?? string.Empty,
                sourceFiles: trees.Select(t => t.FilePath).ToArray(),
                projectReferences: Array.Empty<string>(),
                packageReferences: Array.Empty<Lintelligent.AnalyzerEngine.ProjectModel.PackageReference>());

            var solution = new Lintelligent.AnalyzerEngine.ProjectModel.Solution(
                filePath: string.Empty,
                name: compilation.AssemblyName ?? "Unknown",
                projects: new[] { project },
                configurations: new[] { "Debug" });

            var projectsByPath = new Dictionary<string, Lintelligent.AnalyzerEngine.ProjectModel.Project>(StringComparer.OrdinalIgnoreCase)
            {
                { project.FilePath, project }
            };
            
            var workspaceContext = new Lintelligent.AnalyzerEngine.Abstractions.WorkspaceContext(
                solution,
                projectsByPath);

            foreach (var analyzer in WorkspaceAnalyzers)
            {
                try
                {
                    var results = analyzer.Analyze(trees, workspaceContext);
                    var descriptor = WorkspaceDescriptorMap[analyzer.Id];

                    foreach (var result in results)
                    {
                        var tree = trees.FirstOrDefault(t => t.FilePath == result.FilePath);
                        if (tree == null) continue;

                        var location = Location.Create(tree, result.Location);
                        var diagnostic = Diagnostic.Create(descriptor, location, result.Message);
                        compilationContext.ReportDiagnostic(diagnostic);
                    }
                }
                catch (Exception ex)
                {
                    ReportInternalError(compilationContext, analyzer.Id, ex.Message);
                }
            }
        });
    }

    /// <summary>
    /// Reports an internal analyzer error as LNT999 (compilation context overload).
    /// </summary>
    private static void ReportInternalError(CompilationAnalysisContext context, string analyzerId, string error)
    {
        var diagnostic = Diagnostic.Create(InternalErrorDescriptor, Location.None, analyzerId, error);
        context.ReportDiagnostic(diagnostic);
    }
#endif

    /// <summary>
    /// Reports an internal analyzer error as LNT999.
    /// </summary>
    private static void ReportInternalError(SyntaxTreeAnalysisContext context, string ruleId, string error)
    {
        var diagnostic = Diagnostic.Create(InternalErrorDescriptor, Location.None, ruleId, error);
        context.ReportDiagnostic(diagnostic);
    }
}
