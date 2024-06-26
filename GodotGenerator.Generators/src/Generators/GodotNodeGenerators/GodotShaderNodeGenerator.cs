﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generator.Attributes;

namespace Generator.Generators;

[Generator]
internal class GodotShaderNodeGenerator : IIncrementalGenerator
{
    public record class SemanticProvider(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol);
    public record class CustomProvider(Compilation Compilation, ImmutableArray<SemanticProvider> Classes, string TargetPath);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fullyQualifiedAttr = typeof(ShaderNodeAttribute).FullName;

        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(fullyQualifiedAttr, SyntacticPredicate, SemanticTransform);

        var projectPath = GeneratorUtil.GetCallingPath(ref context);

        var provider = context.CompilationProvider.Combine(classes.Collect()).Combine(projectPath).Select(
            (x, _) => new CustomProvider(x.Left.Left, x.Left.Right, x.Right)
        );

        context.RegisterSourceOutput(provider, ShaderNodeGenerator);
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
    public static void ShaderNodeGenerator(SourceProductionContext context, CustomProvider provider)
    {
        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(ShaderNodeAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");

        var sb = new StringBuilderSG();
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            // Get Attribute Informations
            AttributeData? attrData = GeneratorUtil.GetAttributeDataIfExist(classSymbol, markerAttrSymbol);
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
                var param = attrParameters[i];
                var value = attrData.ConstructorArguments[i].Value;

                if (value == null)
                    continue;

                switch (param.Name)
                {
                    case nameof(ShaderNodeAttribute.chacheShaderMaterialProp): arg_chacheShaderMaterialProp = (bool)value; break;
                    case nameof(ShaderNodeAttribute.chacheShaderScript): arg_chacheShaderScript = (bool)value; break;
                    case nameof(ShaderNodeAttribute.materialMemberName): arg_materialMemberName = (string)value; break;
                    case nameof(ShaderNodeAttribute.shaderScriptPath): arg_shaderScriptPath = (string)value; break;
                    case nameof(ShaderNodeAttribute.isVisualShader): arg_isVisualShader = (bool)value; break;
                }
            }

            //check if class is valid node
            if (!classSymbol.IsOfBaseType(nodeSymbol))
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

            context.AddSource($"{className}.{nameof(ShaderNodeGenerator)}.g.cs", sb.ToString());
            sb.Clear();
        }

    }


}
