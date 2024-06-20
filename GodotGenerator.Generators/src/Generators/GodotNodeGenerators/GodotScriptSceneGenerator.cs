using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generator.Attributes;

namespace Generator.Generators;

[Generator]
internal class GodotScriptSceneGenerator : IIncrementalGenerator
{
    public record class SemanticProvider(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol);
    public record class CustomProvider(Compilation Compilation, ImmutableArray<SemanticProvider> Classes, string TargetPath);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttr = typeof(ScriptSceneAttribute).FullName;

        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(fullyQualifiedAttr, SyntacticPredicate, SemanticTransform);

        var projectPath = GeneratorUtil.GetCallingPath(ref context);

        var provider = context.CompilationProvider.Combine(classes.Collect()).Combine(projectPath).Select(
            (x, _) => new CustomProvider(x.Left.Left, x.Left.Right, x.Right)
        );

        context.RegisterSourceOutput(provider, ScriptSceneGenerator);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } candidate
            && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)
            && !x.IsKind(SyntaxKind.AbstractKeyword)
            && !x.IsKind(SyntaxKind.StaticKeyword)
            && !x.IsKind(SyntaxKind.RecordKeyword));
    }

    private static SemanticProvider SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var candidate = (ClassDeclarationSyntax)context.TargetNode;
        var symbol = context.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken) ?? throw new Exception("Candidate is not a symbol");
        return new(candidate, symbol);
    }

    public static void ScriptSceneGenerator(SourceProductionContext context, CustomProvider provider)
    {
        const string GD_PACKED = GodotUtil.GD_G_NAMESPACE + ".PackedScene";
        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(ScriptSceneAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");
        var resourceSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Resource");

        var sb = new StringBuilderSG();
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            AttributeData? attrData = GeneratorUtil.GetAttributeDataIfExist(classSymbol, markerAttrSymbol);
            if (attrData is null)
                continue;

            bool arg_chachePackedScene = false;
            string arg_scenePath = "";
            foreach (var ctorArg in attrData.ConstructorArguments)
            {
                if (ctorArg.Value is bool boolValue)
                    arg_chachePackedScene = boolValue;
                else if (ctorArg.Value is string stringValue)
                    arg_scenePath = stringValue;
            }

            bool isNode = classSymbol.IsOfBaseType(nodeSymbol);
            bool isResource = classSymbol.IsOfBaseType(resourceSymbol);

            if (!isNode && !isResource)
            {
                context.NewDiagnostic(classSyntax.GetLocation(), 1, $"The class is not a node or resource");
                continue;
            }

            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var ns = classSymbol.ContainingNamespace.ToString();
            if (!string.IsNullOrWhiteSpace(ns))
                sb.AddNamespaceFileScoped(ns);

            var closeClass = sb.CreateBracketDeclaration($"partial class {className}");

            string filePath = arg_scenePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath);

                if (isNode)
                    filePath = Path.ChangeExtension(filePath, GodotUtil.SCENE_EXT);
                else if (isResource)
                    filePath = Path.ChangeExtension(filePath, GodotUtil.RESOURCE_EXT);
            }

            if (isNode)
            {
                if (arg_chachePackedScene)
                {
                    const string VAR_NAME = "Scene";
                    sb.AppendLineC($"public static {GD_PACKED} {VAR_NAME} {{ get; }} = {GD_LOADER}.Load<{GD_PACKED}>(\"{filePath}\")");
                    sb.AppendLineC($"public static {className} Instantiate() => {VAR_NAME}.Instantiate<{className}>()");
                }
                else
                {
                    sb.AppendLineC($"public static {className} Instantiate() => {GD_LOADER}.Load<{GD_PACKED}>(\"{filePath}\").Instantiate<{className}>()");
                }
            }
            else if (isResource)
            {
                if (arg_chachePackedScene)
                    sb.AppendLineC($"public static {className} Resource {{ get; }} = {GD_LOADER}.Load<{className}>(\"{filePath}\")");
                else
                    sb.AppendLineC($"public static {className} GetResource() => {GD_LOADER}.Load<{className}>(\"{filePath}\")");
            }

            closeClass();

            //OutputWriter.WriteFileOnProject(sb.ToString(), "Tests\\" + className + ".txt");
            context.AddSource($"{className}.{nameof(ScriptSceneGenerator)}.g.cs", sb.ToString());
            sb.Clear();
        }
    }


}
