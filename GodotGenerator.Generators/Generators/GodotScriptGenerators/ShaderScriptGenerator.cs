using Generator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Generator.Generators;

[Generator]
internal class ShaderScriptGenerator : IIncrementalGenerator
{
    public record class SymbolsProvider(INamedTypeSymbol AttrSymbol, INamedTypeSymbol NodeSymbol);
    public record class SemanticProvider(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol);
    public record class CustomProvider(ImmutableArray<SemanticProvider> Classes, SymbolsProvider Symbols, string TargetPath);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttr = typeof(ShaderScriptAttribute).FullName;

        var projectPath = GeneratorUtil.GetCallingPath(ref context);

        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(fullyQualifiedAttr, Predicate, Transform).Collect();

        static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } candidate
                && candidate.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)
                && !x.IsKind(SyntaxKind.AbstractKeyword)
                && !x.IsKind(SyntaxKind.StaticKeyword)
                && !x.IsKind(SyntaxKind.RecordKeyword));
        }
        static SemanticProvider Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var candidate = (ClassDeclarationSyntax)context.TargetNode;
            var symbol = context.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken) ?? throw new Exception("Candidate is not a symbol");
            return new(candidate, symbol);
        }

        var symbols = context.CompilationProvider.Select((comp, _) =>
        {
            var markerAttrSymbol = comp.GetSymbolByName(fullyQualifiedAttr);
            var nodeSymbol = comp.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");
            return new SymbolsProvider(markerAttrSymbol, nodeSymbol);
        });

        var provider = projectPath.Combine(classes).Combine(symbols).Select((x, _) => new CustomProvider(x.Left.Right, x.Right, x.Left.Left));

        context.RegisterSourceOutput(provider, Generator);
    }

    public static void Generator(SourceProductionContext context, CustomProvider provider)
    {

        var sb = new StringBuilderSG();
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            // Get Attribute Informations
            AttributeData? attrData = GeneratorUtil.GetAttributeDataIfExist(classSymbol, provider.Symbols.AttrSymbol);
            if (attrData is null || attrData.AttributeConstructor is null)
                continue;

            var arg_chacheShaderMaterialProp = false;
            var arg_chacheShaderScript = false;
            var arg_materialMemberName = "";
            var arg_shaderScriptPath = "";
            var arg_isVisualShader = false;

            var attrParameters = attrData.AttributeConstructor.Parameters;
            for (int i = 0; i < attrParameters.Length; i++)
            {
                var value = attrData.ConstructorArguments[i].Value;

                if (value == null)
                    continue;

                var param = attrParameters[i];
                switch (param.Name)
                {
                    case nameof(ShaderScriptAttribute.chacheShaderMaterialProp): arg_chacheShaderMaterialProp = (bool)value; break;
                    case nameof(ShaderScriptAttribute.chacheShaderScript): arg_chacheShaderScript = (bool)value; break;
                    case nameof(ShaderScriptAttribute.materialMemberName): arg_materialMemberName = (string)value; break;
                    case nameof(ShaderScriptAttribute.shaderScriptPath): arg_shaderScriptPath = (string)value; break;
                    case nameof(ShaderScriptAttribute.isVisualShader): arg_isVisualShader = (bool)value; break;
                }
            }

            //check if class is valid node
            if (!classSymbol.IsOfBaseType(provider.Symbols.NodeSymbol))
            {
                context.NewDiagnostic(classSyntax.GetLocation(), 1, $"The class is not a node");
                continue;
            }

            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var ns = classSymbol.ContainingNamespace.ToString();
            if (!string.IsNullOrWhiteSpace(ns))
                sb.AddNamespaceFileScoped(ns);

            var closeClass = sb.CreateBracketDeclaration($"partial class {className}");

            //FilePath
            const string VisualShader_EXT = GodotUtil.RESOURCE_EXT;

            const string Shader_TYPE = GodotUtil.GD_G_NAMESPACE + ".Shader";
            const string VisualShader_TYPE = GodotUtil.GD_G_NAMESPACE + ".VisualShader";

            string filePath, scriptType;

            if (string.IsNullOrWhiteSpace(arg_shaderScriptPath))
            {
                filePath = GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath);

                if (arg_isVisualShader)
                {
                    filePath = Path.ChangeExtension(filePath, VisualShader_EXT);
                    scriptType = VisualShader_TYPE;
                }
                else
                {
                    filePath = Path.ChangeExtension(filePath, GodotUtil.SHADER_EXT);
                    scriptType = Shader_TYPE;
                }
            }
            else
            {
                filePath = arg_shaderScriptPath;
                switch (Path.GetExtension(filePath))
                {
                    case GodotUtil.SHADER_EXT: scriptType = Shader_TYPE; break;
                    case VisualShader_EXT: scriptType = VisualShader_TYPE; break;
                    default: context.NewDiagnostic(classSyntax.GetLocation(), 2, $"Invalid shader script extension"); continue;
                }
            }

            const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";
            const string SHADER_VAR_NAME = "ShaderScript";
            if (arg_chacheShaderScript)
            {
                sb.AppendLineC($"public static {scriptType} {SHADER_VAR_NAME} {{ get; }} = {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }
            else
            {
                sb.AppendLineC($"public static {scriptType} {SHADER_VAR_NAME} => {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }

            sb.AppendLine();

            const string GD_ShaderMaterial = GodotUtil.GD_G_NAMESPACE + ".ShaderMaterial";
            const string MATERIAL_VAR_NAME = "ShaderMaterial";
            if (arg_chacheShaderMaterialProp)
            {
                var memberName = StringUtil.FirstCharToLowerCase(SHADER_VAR_NAME);
                sb.AppendLine($"//the variable needs to be set be the user");
                sb.AppendLineC($"protected {GD_ShaderMaterial} {memberName}");
                sb.AppendLineC($"public {GD_ShaderMaterial} {MATERIAL_VAR_NAME} => {memberName}");
            }
            else
            {
                sb.AppendLineC($"public {GD_ShaderMaterial} {MATERIAL_VAR_NAME} => ({GD_ShaderMaterial}){arg_materialMemberName}");
            }

            sb.AppendLine();
            closeClass();

            context.AddSource($"{className}.{nameof(ShaderScriptGenerator)}.g.cs", sb.ToString());
            sb.Clear();
        }

    }


}
