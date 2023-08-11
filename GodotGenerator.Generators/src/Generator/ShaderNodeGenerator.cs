using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

using Generator.Attributes;

namespace Generator.Generators;

[Generator]
internal class ShaderNodeGenerator : IIncrementalGenerator
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
        const string INTERFACE_NAME = "IShaderNode";
        const string INTERFACE_VAR_NAME = "ShaderMaterial";
        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(ShaderNodeAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");

        var sb = new StringBuilderSG();
        var namedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            if (!namedSymbols.Add(classSymbol))
                continue;

            // Get Attribute Informations
            bool hasAttr = false;

            var arg_chacheShaderMaterialProp = false;
            var arg_chacheShaderScript = false;
            var arg_shaderScriptPath = "";
            var arg_visualShader = false;

            foreach (var attr in classSymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttrSymbol))
                    continue;
                else
                    hasAttr = true;

                var parameters = attr.AttributeConstructor!.Parameters;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var value = attr.ConstructorArguments[i].Value;

                    if (value == null)
                        continue;

                    switch (param.Name)
                    {
                        case nameof(ShaderNodeAttribute.chacheShaderMaterialProp): arg_chacheShaderMaterialProp = (bool)value; break;
                        case nameof(ShaderNodeAttribute.chacheShaderScript): arg_chacheShaderScript = (bool)value; break;
                        case nameof(ShaderNodeAttribute.shaderScriptPath): arg_shaderScriptPath = (string)value; break;
                        case nameof(ShaderNodeAttribute.visualShader): arg_visualShader = (bool)value; break;
                    }
                }
            }

            if (!hasAttr)
                continue;

            //check if class is a Godot.Node
            if (!classSymbol.IsOfBaseType(nodeSymbol))
            {
                context.NewDiagnostic(classSyntax.GetLocation(), 1, $"The class is not a node");
                continue;
            }

            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            sb.AddUsing(GodotUtil.GD_NAMESPACE);
            sb.AppendLine();

            sb.AddNullable(true);
            sb.AppendLine();

            var ns = classSymbol.ContainingNamespace.Name;
            if (!string.IsNullOrWhiteSpace(ns))
                sb.AddNamespaceFileScoped(ns);

            var closeClass = sb.CreateBracketDeclaration($"partial class {className} : {INTERFACE_NAME}");

            string filePath = string.IsNullOrWhiteSpace(arg_shaderScriptPath)
                ? GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath)
                : arg_shaderScriptPath;

            var scriptType = GodotUtil.GD_G_NAMESPACE + (arg_visualShader ? ".Shader" : ".VisualShader");
            if (arg_chacheShaderScript)
            {
                sb.AppendLineC($"public static {scriptType} ShaderScript {{ get; }} = {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }
            else
            {
                sb.AppendLineC($"public static {scriptType} GetShaderScript() => {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }

            var smFullName = GodotUtil.GD_G_NAMESPACE + ".ShaderMaterial";
            if (arg_chacheShaderMaterialProp)
            {
                var memberName = UtilString.FirstCharToLowerCase(INTERFACE_VAR_NAME);
                sb.AppendLine($"protected {smFullName} {memberName}");
                sb.AppendLine($"public {smFullName} {INTERFACE_VAR_NAME} => {memberName}");
            }
            else
            {
                sb.AppendLineC($"public {smFullName} {INTERFACE_VAR_NAME} => ({smFullName})Material");
            }

            sb.AppendLine();
            closeClass();

            context.AddSource(className + ".g.cs", sb.ToString());
            sb.Clear();
        }

        //Create Shader Node Interface
        {
            sb.AddNamespaceFileScoped(GodotUtil.GD_NAMESPACE);
            //sb.AddNamespaceFileScoped(typeof(ShaderNodeAttribute).Namespace);
            var closeClass = sb.CreateBracketDeclaration($"public interface {INTERFACE_NAME}");
            sb.AppendLine($"{GodotUtil.GD_G_NAMESPACE}.ShaderMaterial {INTERFACE_VAR_NAME} {{ get; }}");
            closeClass();

            context.AddSource(INTERFACE_NAME + ".g.cs", sb.ToString());
            sb.Clear();
        }
    }


}
