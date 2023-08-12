using Microsoft.CodeAnalysis;

namespace Generator;

internal static class ErrorUtil
{
    public static void NewDiagnostic(this ref GeneratorExecutionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static void NewDiagnostic(this ref SourceProductionContext context, Location location, DiagnosticDescriptor dd)
        => context.ReportDiagnostic(Diagnostic.Create(dd, location));

    public static void NewDiagnostic(this ref SourceProductionContext context, Location location, int id, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        var description = new DiagnosticDescriptor("SG" + id.ToString("0000"), "Inspection", message, "SG.Parsing", severity, true);
        var diagnostic = Diagnostic.Create(description, location);
        context.ReportDiagnostic(diagnostic);
    }
}