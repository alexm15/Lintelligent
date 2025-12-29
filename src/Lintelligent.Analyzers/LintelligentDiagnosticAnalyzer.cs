using System.Diagnostics;
using System.Reflection;
using Lintelligent.AnalyzerEngine.Results;
using Lintelligent.AnalyzerEngine.Rules;
using Lintelligent.AnalyzerEngine.Rules.Monad;
using Lintelligent.Analyzers.Adapters;
using Lintelligent.Analyzers.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lintelligent.Analyzers;

/// <summary>
///     Main Roslyn diagnostic analyzer that wraps all Lintelligent IAnalyzerRule implementations.
/// </summary>
/// <remarks>
///     This analyzer:
///     - Discovers all IAnalyzerRule implementations via reflection at initialization
///     - Creates DiagnosticDescriptor for each rule
///     - Executes all rules on each syntax tree during compilation
///     - Supports EditorConfig severity overrides (dotnet_diagnostic.{ruleId}.severity)
///     - Skips generated code automatically
///     - Reports internal errors as LNT999 without crashing
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LintelligentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly IAnalyzerRule[] Rules = DiscoverRules();
    private static readonly DiagnosticDescriptor[] Descriptors = Rules.Select(RuleDescriptorFactory.Create).ToArray();

    private static readonly Dictionary<string, DiagnosticDescriptor> DescriptorMap =
        Descriptors.ToDictionary(d => d.Id, StringComparer.Ordinal);

    // Internal error descriptor for LNT999
    private static readonly DiagnosticDescriptor InternalErrorDescriptor = new(
        "LNT999",
        "Internal Analyzer Error",
        "Analyzer error in {0}: {1}",
        "InternalError",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "An internal error occurred while running the analyzer. This may indicate a bug in the analyzer.");

    /// <summary>
    ///     Gets the set of diagnostic descriptors supported by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.Append(InternalErrorDescriptor).ToArray());

    /// <summary>
    ///     Initializes the analyzer and registers callbacks.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); // Skip generated code
        context.EnableConcurrentExecution(); // Thread-safe parallel execution
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Check if LanguageExt.Core is referenced (required for monad detection)
            var hasLanguageExt = compilationContext.Compilation.ReferencedAssemblyNames
                .Any(name => name.Name.Equals("LanguageExt.Core", StringComparison.OrdinalIgnoreCase));

            compilationContext.RegisterSyntaxTreeAction(treeContext =>
                AnalyzeSyntaxTree(treeContext, hasLanguageExt));
        });
    }

    /// <summary>
    ///     Analyzes a syntax tree by executing all rules.
    /// </summary>
    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, bool hasLanguageExt)
    {
        var configOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);

        // Parse monad detection configuration once per syntax tree
        var monadOptions = MonadDetectionOptions.Parse(configOptions);

        // Auto-enable monad detection if LanguageExt.Core is referenced (unless explicitly disabled)
        var monadDetectionEnabled = hasLanguageExt &&
                                    (monadOptions.Enabled ||
                                     !configOptions.TryGetValue("language_ext_monad_detection", out _));

        foreach (IAnalyzerRule rule in Rules)
        {
            try
            {
                // Skip monad rules if monad detection is not enabled or LanguageExt.Core not referenced
                if (IsMonadRule(rule) && !monadDetectionEnabled)
                    continue;

                // Check EditorConfig for severity override
                if (configOptions.TryGetValue($"dotnet_diagnostic.{rule.Id}.severity", out var severity) && SeverityMapper.IsSuppressed(severity))
                    continue; // Suppressed via EditorConfig

                // Execute rule analysis
                IEnumerable<DiagnosticResult> results = rule.Analyze(context.Tree);
                DiagnosticDescriptor? descriptor = DescriptorMap[rule.Id];

                foreach (DiagnosticResult? result in results)
                {
                    Diagnostic diagnostic = DiagnosticConverter.Convert(result, context.Tree, descriptor);
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
    ///     Discovers all IAnalyzerRule implementations in the AnalyzerEngine assembly.
    /// </summary>
    private static IAnalyzerRule[] DiscoverRules()
    {
        Type ruleInterface = typeof(IAnalyzerRule);
        Assembly assembly = ruleInterface.Assembly;

        IEnumerable<Type> ruleTypes = assembly.GetTypes()
            .Where(t => ruleInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        var rules = new List<IAnalyzerRule>();
        foreach (Type? ruleType in ruleTypes)
        {
            try
            {
                var rule = (IAnalyzerRule)Activator.CreateInstance(ruleType)!;
                rules.Add(rule);
            }
            catch (Exception ex)
            {
                // Log discovery failure (MSBuild diagnostic output)
                Debug.WriteLine($"[Lintelligent] Failed to load rule {ruleType.Name}: {ex.Message}");
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
    ///     Checks if a rule is a monad detection rule (LNT200-LNT299 range).
    /// </summary>
    private static bool IsMonadRule(IAnalyzerRule rule)
    {
        // Monad rules are in the LNT200-LNT299 ID range
        return rule.Id.StartsWith("LNT2", StringComparison.Ordinal) &&
               rule.Id.Length == 6 &&
               int.TryParse(rule.Id.Substring(3), System.Globalization.NumberStyles.Integer,
                   System.Globalization.CultureInfo.InvariantCulture, out var number) &&
               number is >= 200 and < 300;
    }

    /// <summary>
    ///     Reports an internal analyzer error as LNT999.
    /// </summary>
    private static void ReportInternalError(SyntaxTreeAnalysisContext context, string ruleId, string error)
    {
        var diagnostic = Diagnostic.Create(InternalErrorDescriptor, Location.None, ruleId, error);
        context.ReportDiagnostic(diagnostic);
    }
}
