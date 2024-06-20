using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

internal static class GeneratorExtension
{

    #region GetSymbolByName

    public static INamedTypeSymbol GetSymbolByName(this ref GeneratorExecutionContext context, string fullName) =>
        context.Compilation.GetSymbolByName(fullName);

    public static INamedTypeSymbol GetSymbolByName(this ref GeneratorSyntaxContext context, string fullName) =>
        context.SemanticModel.Compilation.GetSymbolByName(fullName);

    public static INamedTypeSymbol GetSymbolByName(this Compilation compilation, string fullName) =>
        compilation.GetTypeByMetadataName(fullName) ?? throw new Exception($"Can't find type: {fullName}");

    #endregion

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

    #region ToString

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

    public static string GetTypeFullName(this ITypeSymbol typeSymbol) => typeSymbol.SpecialType == SpecialType.None ?
        typeSymbol.ToDisplayString() : typeSymbol.SpecialType.ToString().Replace("_", ".");

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

    public static string ToGlobalName(this ISymbol symbol) => symbol.ToDisplayString(GlobalTypeNullableFormat);

    #endregion

}
