using System.Diagnostics.CodeAnalysis;

// RS2008: Analyzer release tracking not yet implemented
[assembly:
    SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking",
        Justification = "Release tracking will be added in future version")]

// S1075: Hardcoded URL is intentional - base documentation URL
[assembly:
    SuppressMessage("Minor Code Smell", "S1075:Refactor your code not to use hardcoded absolute paths or URIs",
        Justification = "Base help URL for analyzer documentation", Scope = "member",
        Target = "~F:Lintelligent.Analyzers.Adapters.RuleDescriptorFactory.BaseHelpUrl")]
