using Microsoft.CodeAnalysis;

namespace Generator;

public static class ErrorsCode
{
    public static void NewDiagnostic(this ref GeneratorExecutionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static void NewDiagnostic(this ref SourceProductionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static DiagnosticDescriptor SG0001(string className) =>
        new(nameof(SG0001), "Inspection", $"The class '{className}' is not partial", "SG.Parsing", DiagnosticSeverity.Warning, true);

    public static DiagnosticDescriptor SG0002(string className) =>
        new(nameof(SG0002), "Inspection", $"The class '{className}' is not a node or resource", "SG.Parsing", DiagnosticSeverity.Warning, true);

    public static DiagnosticDescriptor SG0003(string className) =>
        new(nameof(SG0003), "Inspection", $"The class '{className}' is not a node", "SG.Parsing", DiagnosticSeverity.Warning, true);

    public static DiagnosticDescriptor SG0004(string className) =>
        new(nameof(SG0004), "Inspection", $"The class '{className}' is not a resource", "SG.Parsing", DiagnosticSeverity.Warning, true);

    public static DiagnosticDescriptor SG0005(string fileName) =>
        new(nameof(SG0005), "Inspection", $"Could not find the file: '{fileName}'", "SG.Parsing", DiagnosticSeverity.Warning, true);

}