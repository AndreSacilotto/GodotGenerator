using Generator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Generator.Generators;

[Generator]
internal sealed class WhatNotificationGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttr = typeof(WhatNotificationAttribute).FullName;
        var fullyQualifiedAttrItem = typeof(WhatNotificationMethodAttribute).FullName;

        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(fullyQualifiedAttr, Predicate, Transform).Collect();

        static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is ClassDeclarationSyntax { BaseList.Types.Count: > 0, Members.Count: > 0 } candidate
                && candidate.Members.Any(x => x is MethodDeclarationSyntax { AttributeLists.Count: > 0 })
                && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)
                && !x.IsKind(SyntaxKind.AbstractKeyword)
                && !x.IsKind(SyntaxKind.StaticKeyword)
                && !x.IsKind(SyntaxKind.RecordKeyword));
        }
        GenerationItem Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var compilation = context.SemanticModel.Compilation;

            var attrSymbol = compilation.GetSymbolByName(fullyQualifiedAttr);

            var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
            var classSymbol = (INamedTypeSymbol)context.TargetSymbol;

            var ns = classSymbol.ContainingNamespace.ToString();
            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var attrData = context.Attributes.Single(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attrSymbol));

            int arg_baseCall = 0;
            foreach (var ctorArg in attrData.ConstructorArguments)
            {
                if (ctorArg.Value is int intValue)
                    arg_baseCall = intValue;
            }

            var attrSymbolItem = compilation.GetSymbolByName(fullyQualifiedAttrItem);

            var valid = new List<AttributeItem>();
            foreach (var item in classSyntax.Members)
            {
                if (item is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodSyntax)
                {
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);
                    if (methodSymbol is null)
                        continue;

                    var attrDataItem = GeneratorUtil.GetAttributeDataIfExist(methodSymbol, attrSymbolItem);
                    if (attrDataItem is null)
                        continue;

                    int arg_what = 0;
                    foreach (var ctorArg in attrDataItem.ConstructorArguments)
                        if (ctorArg.Value is int intValue)
                            arg_what = intValue;

                    valid.Add(new(methodSyntax.Identifier.ToFullString(), arg_what));
                }
            }

            return new GenerationItem(className, ns, arg_baseCall, new(valid.ToArray()));
        }

        // Output
        context.RegisterSourceOutput(provider, Generate);
    }

    private record struct AttributeItem(string MethodName, int What);
    private record class GenerationItem(string ClassName, string ClassNamespace, int ArgBaseCall, EquatableArray<AttributeItem> Attributes);

    private static void Generate(SourceProductionContext context, ImmutableArray<GenerationItem> items)
    {
        var sb = new StringBuilderSG();
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.ClassNamespace))
                sb.AddNamespaceFileScoped(item.ClassNamespace);

            sb.AppendLine($"partial class {item.ClassName}");
            sb.OpenBracket();

            sb.AppendLine("public override void _Notification(int what)");
            sb.OpenBracket();

            const string BaseCall = "base._Notification(what)";
            if (item.ArgBaseCall < 0)
                sb.AppendLineC(BaseCall);

            var closeSwitch = sb.CreateBracketDeclaration("switch (what)");

            foreach (var args in item.Attributes)
                sb.AppendLine($"case {args.What}: {args.MethodName}(); break;");

            sb.CloseBracket();

            if (item.ArgBaseCall > 0)
                sb.AppendLineC(BaseCall);

            sb.CloseBracket();
            sb.CloseBracket();

            context.AddSource($"{item.ClassName}.{nameof(WhatNotificationGenerator)}.g.cs", sb.ToString());
            sb.Clear();
        }
    }


}
