using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

public static class UtilGenerator
{
    public static readonly string[] NewLines = new string[3] { "\r\n", "\r", "\n" };

    #region GetSymbolByName

    public static INamedTypeSymbol GetSymbolByName(this ref GeneratorExecutionContext context, string fullName) =>
        context.Compilation.GetSymbolByName(fullName);

    public static INamedTypeSymbol GetSymbolByName(this ref GeneratorSyntaxContext context, string fullName) =>
        context.SemanticModel.Compilation.GetSymbolByName(fullName);

    public static INamedTypeSymbol GetSymbolByName(this Compilation compilation, string fullName) =>
        compilation.GetTypeByMetadataName(fullName) ?? throw new Exception($"Can't find type: {fullName}");

    #endregion

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

    #region Extensions - To String

    public static string ToFullyQualifiedName(this TypeSyntax typeSyntax, SemanticModel model)
    {
        var info = model.GetTypeInfo(typeSyntax);
        var typeSymbol = info.ConvertedType;
        //for some reason this dont work on nullable
        //var typeNullable = info.ConvertedNullability;

        string typeName;
        if (typeSymbol is not null)
        {
            typeName = typeSymbol.ToGlobalName();
            //if (typeSyntax is NullableTypeSyntax && typeName[typeName.Length - 1] != '?')
            //    typeName += '?';
        }
        else
            typeName = typeSyntax.ToString();
        return typeName;
    }

    #region Format
    public static readonly SymbolDisplayFormat GlobalTypeNullableFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: 
            SymbolDisplayGenericsOptions.IncludeTypeParameters | 
            SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions:
            SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeType |
            SymbolDisplayMemberOptions.IncludeRef |
            SymbolDisplayMemberOptions.IncludeContainingType,
        kindOptions:
            SymbolDisplayKindOptions.IncludeMemberKeyword,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeModifiers |
            SymbolDisplayParameterOptions.IncludeType |
            SymbolDisplayParameterOptions.IncludeName |
            SymbolDisplayParameterOptions.IncludeDefaultValue,
        localOptions: SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );
    #endregion

    public static string ToGlobalName(this ISymbol symbol) => symbol.ToDisplayString(GlobalTypeNullableFormat);


    #endregion

    #region Extesion - Util

    public static bool IsOfBaseType(this ITypeSymbol type, ITypeSymbol baseType)
    {
        if (type is ITypeParameterSymbol p)
            return p.ConstraintTypes.Any(ct => ct.IsOfBaseType(baseType));

        var t = type.BaseType;
        while (t != null)
        {
            if (SymbolEqualityComparer.Default.Equals(t, baseType))
                return true;
            t = t.BaseType;
        }

        return false;
    }

    public static string AccessibilityToString(this Accessibility accessibility) => accessibility switch
    {
        Accessibility.Private => "private",
        Accessibility.Public => "public",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedAndInternal => "protected internal",
        Accessibility.ProtectedOrInternal => "internal",
        _ => throw new Exception($"There isnt a {accessibility}"),
    };

    public static object? GetAttributeValueByName(this AttributeData attributeData, string argName)
    {
        if (attributeData.AttributeConstructor == null)
            return null;

        var parameters = attributeData.AttributeConstructor.Parameters;

        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].Name == argName)
                return attributeData.ConstructorArguments[i].Value;
        return null;
    }

    #endregion

}
