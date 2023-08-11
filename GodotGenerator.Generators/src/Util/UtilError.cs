using Microsoft.CodeAnalysis;

namespace Generator;

internal static class UtilError
{
    public static void NewDiagnostic(this ref GeneratorExecutionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static void NewDiagnostic(this ref SourceProductionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static void NewDiagnostic(this ref SourceProductionContext context, Location location, int id, string message, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        var description = new DiagnosticDescriptor("SG" + id, "Inspection", message, "SG.Parsing", severity, true);
        var diagnostic = Diagnostic.Create(description, location);
        context.ReportDiagnostic(diagnostic);
    }
}