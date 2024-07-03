using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Generator.Generators;

[Generator]
internal class ProtectedEventGenerator : IIncrementalGenerator
{
    private record class SemanticProvider(FieldDeclarationSyntax Syntax, IFieldSymbol Symbol);
    private record class CustomProvider(ISymbol AttrSymbol, ImmutableArray<SemanticProvider> Fields);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttr = typeof(Attributes.ProtectedEventAttribute).FullName;

        var markerAttrSymbol = context.CompilationProvider.Select((comp, _) => comp.GetSymbolByName(fullyQualifiedAttr));//"Generator.Attributes.ProtectedEventAttribute");

        var fields = context.SyntaxProvider.ForAttributeWithMetadataName(fullyQualifiedAttr, SyntacticPredicate, SemanticTransform);

        var provider = markerAttrSymbol.Combine(fields.Collect()).Select((x, _) => new CustomProvider(x.Left, x.Right));

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is FieldDeclarationSyntax { Declaration.Variables.Count: 1 } candidate && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword));
    }

    private static SemanticProvider SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var candidate = (FieldDeclarationSyntax)context.TargetNode;

        var variableDSyntax = candidate.Declaration.Variables.FirstOrDefault() ?? throw new Exception("Candidate has no variables");
        var symbol = context.SemanticModel.GetDeclaredSymbol(variableDSyntax, cancellationToken) ?? throw new Exception("Candidate is not a symbol");

        return new(candidate, (IFieldSymbol)symbol);
    }

    private static void Execute(SourceProductionContext context, CustomProvider provider)
    {

        var sb = new StringBuilderSG();
        foreach (var fieldItem in provider.Fields)
        {
            var fieldSyntax = fieldItem.Syntax;
            var fieldSymbol = fieldItem.Symbol;

            bool hasAttr = false;
            bool arg_nullable = true;
            string arg_eventName = "";
            foreach (var attr in fieldSymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, provider.AttrSymbol))
                    continue;

                foreach (var ctorArg in attr.ConstructorArguments)
                {
                    if (ctorArg.Value is bool boolValue)
                        arg_nullable = boolValue;
                    else if (ctorArg.Value is string stringValue)
                        arg_eventName = stringValue;
                }
                hasAttr = true;
            }

            if (!hasAttr)
                continue;

            var ns = fieldSymbol.ContainingNamespace.ToString();
            var className = fieldSymbol.ContainingType.Name;
            var classPartialName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var fieldName = fieldSymbol.Name;
            var nu = fieldSymbol.NullableAnnotation;
            var fieldType = fieldSymbol.Type.ToGlobalName();

            if (string.IsNullOrWhiteSpace(arg_eventName))
            {
                var name = fieldSymbol.Name;
                arg_eventName = name[0].ToString().ToUpper() + name.Substring(1);
            }

            sb.AddUsing(GodotUtil.GD_NAMESPACE);
            sb.AppendLine();

            sb.AddNullable(true);
            sb.AppendLine();

            Action? closeNamespace = null;
            if (!string.IsNullOrWhiteSpace(ns))
                closeNamespace = sb.CreateNamespace(ns);

            var closeType = sb.CreateBracketDeclaration($"partial class {classPartialName}");

            sb.AppendLine($"public event {fieldType} {arg_eventName} {{ add => {fieldName} += value; remove => {fieldName} -= value;  }}");

            closeType();
            closeNamespace?.Invoke();

            context.AddSource($"{className}.{arg_eventName}.g.cs", sb.ToString());
            sb.Clear();
        }

    }

}
