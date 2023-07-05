﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Generator.Generator;

[Generator]
public class ProtectedEventGenerator : IIncrementalGenerator
{
    private record class SemanticProvider(FieldDeclarationSyntax Syntax, IFieldSymbol Symbol);
    private record class CustomProvider(Compilation Compilation, ImmutableArray<SemanticProvider> Fields);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fields = context.SyntaxProvider.CreateSyntaxProvider(SyntacticPredicate, SemanticTransform);

        var provider = context.CompilationProvider.Combine(fields.Collect()).Select((x, _) => new CustomProvider(x.Left, x.Right));

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        //if (syntaxNode is not ClassDeclarationSyntax { Members.Count: > 0 } classSyntax)
        //    return false;
        //var fields = classSyntax.Members.Where(
        //    member => member.IsKind(SyntaxKind.FieldDeclaration) &&
        //    member.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword))
        //);
        //return fields.Count() > 0;

        return syntaxNode is FieldDeclarationSyntax { AttributeLists.Count: > 0, Declaration.Variables.Count: 1 } candidate
            && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword));
    }

    private static SemanticProvider SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var candidate = (FieldDeclarationSyntax)context.Node;

        var variableDSyntax = candidate.Declaration.Variables.FirstOrDefault() ?? throw new Exception("Candidate has no variables");
        var symbol = context.SemanticModel.GetDeclaredSymbol(variableDSyntax, cancellationToken) ?? throw new Exception("Candidate is not a symbol");

        return new(candidate, (IFieldSymbol)symbol);
    }

    private static void Execute(SourceProductionContext context, CustomProvider provider)
    {
        var markerAttrSymbol = provider.Compilation.GetSymbolByName("Generator.Attributes.ProtectedEventAttribute");

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
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttrSymbol))
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

            var ns = fieldSymbol.ContainingNamespace.Name;
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

            sb.AddUsing("Godot");
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
