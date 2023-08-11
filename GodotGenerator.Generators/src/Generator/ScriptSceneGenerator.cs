using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Generator.Generators;

[Generator]
internal class ScriptSceneGenerator : IIncrementalGenerator
{
    private record class SemanticProvider(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol);
    private record class CustomProvider(Compilation Compilation, ImmutableArray<SemanticProvider> Classes, string TargetPath);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(SyntacticPredicate, SemanticTransform);

        var projectPath = UtilGenerator.GetCallingPath(ref context);

        var provider = context.CompilationProvider.Combine(classes.Collect()).Combine(projectPath).Select(
            (x, _) => new CustomProvider(x.Left.Left, x.Left.Right, x.Right)
        );

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0, BaseList.Types.Count: > 0 } candidate
            && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)
            && !x.IsKind(SyntaxKind.AbstractKeyword)
            && !x.IsKind(SyntaxKind.StaticKeyword)
            && !x.IsKind(SyntaxKind.RecordKeyword));
    }

    private static SemanticProvider SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var candidate = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken) ?? throw new Exception("Candidate is not a symbol");
        return new(candidate, symbol);
    }

    private static void Execute(SourceProductionContext context, CustomProvider provider)
    {
        const string GD_PACKED = GodotUtil.GD_G_NAMESPACE + ".PackedScene";
        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(Attributes.ScriptSceneAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");
        var resourceSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Resource");

        var sb = new StringBuilderSG();
        var namedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            if (!namedSymbols.Add(classSymbol))
                continue;

            bool hasAttr = false;
            bool arg_chachePackedScene = false;
            string arg_resPath = "";
            foreach (var attr in classSymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttrSymbol))
                    continue;

                foreach (var ctorArg in attr.ConstructorArguments)
                {
                    if (ctorArg.Value is bool boolValue)
                        arg_chachePackedScene = boolValue;
                    else if (ctorArg.Value is string stringValue)
                        arg_resPath = stringValue;
                }
                hasAttr = true;
            }

            if (!hasAttr)
                continue;

            bool isNode = classSymbol.IsOfBaseType(nodeSymbol);
            bool isResource = classSymbol.IsOfBaseType(resourceSymbol);

            var ns = classSymbol.ContainingNamespace.Name;
            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            sb.AddUsing(GodotUtil.GD_NAMESPACE);
            sb.AppendLine();

            sb.AddNullable(true);
            sb.AppendLine();

            Action? closeNamespace = null;
            if (!string.IsNullOrWhiteSpace(ns))
                closeNamespace = sb.CreateNamespace(ns);

            var closeType = sb.CreateBracketDeclaration($"partial class {className}");

            string filePath;
            if (string.IsNullOrWhiteSpace(arg_resPath))
            {
                filePath = GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath);
                if (isNode)
                    filePath = Path.ChangeExtension(filePath, ".tscn");
                else if (isResource)
                    filePath = Path.ChangeExtension(filePath, ".tres");
            }
            else
                filePath = arg_resPath;

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
                    sb.AppendLineC($"public static {className} GetUniqueResource() => {GD_LOADER}.Load<{className}>(\"{filePath}\")");
            }
            else
                context.NewDiagnostic(classSyntax.GetLocation(), 2, $"{className} is not a node or resource", DiagnosticSeverity.Error);

            sb.AppendLine();
            sb.AppendLine($"private {className}() {{ }}");

            closeType();
            closeNamespace?.Invoke();

            //OutputWriter.WriteFileOnProject(sb.ToString(), "Tests\\" + className + ".txt");
            context.AddSource(className + ".g.cs", sb.ToString());
            sb.Clear();
        }
    }

}
