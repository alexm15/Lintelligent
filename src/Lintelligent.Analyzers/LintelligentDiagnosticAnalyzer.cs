using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
        => ImmutableArray.Create(Descriptors.Append(InternalErrorDescriptor).ToArray());

    /// <summary>
    /// Initializes the analyzer and registers callbacks.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);  // Skip generated code
        context.EnableConcurrentExecution();  // Thread-safe parallel execution
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
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

    /// <summary>
    /// Reports an internal analyzer error as LNT999.
    /// </summary>
    private static void ReportInternalError(SyntaxTreeAnalysisContext context, string ruleId, string error)
    {
        var diagnostic = Diagnostic.Create(InternalErrorDescriptor, Location.None, ruleId, error);
        context.ReportDiagnostic(diagnostic);
    }
}
