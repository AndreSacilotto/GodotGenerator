using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

internal static class GeneratorUtil
{
    //BaseNamespaceDeclarationSyntax on .Net6 (because you can use higher lib version)
    public static string GetNamespaceFrom(SyntaxNode syntax) => syntax.Parent switch
    {
        NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
        null => string.Empty, // or whatever you want to do
        _ => GetNamespaceFrom(syntax.Parent)
    };

    #region Project Path
    private const string BUILD_PROJECT_DIR = "build_property.projectdir";
    public static string GetCallingPath(ref GeneratorExecutionContext context)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BUILD_PROJECT_DIR, out var result) && result != null && !string.IsNullOrWhiteSpace(result))
            return result;
        return string.Empty;
    }
    public static IncrementalValueProvider<string> GetCallingPath(ref IncrementalGeneratorInitializationContext context) => context.AnalyzerConfigOptionsProvider.Select((x, cancelTK) =>
    {
        x.GlobalOptions.TryGetValue(BUILD_PROJECT_DIR, out var result);
        return result ?? string.Empty;
    });
    #endregion

    public static AttributeData? GetAttributeDataIfExist(ISymbol typeSymbol, ISymbol attributeSymbol)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                continue;
            return attr;
        }
        return null;
    }

}
