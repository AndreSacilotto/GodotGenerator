using Generator.Attributes;
using Microsoft.CodeAnalysis;

namespace Generator.Generators;

partial class GodotNodeResourceClassGenerator
{
    public static void ShaderNodeGenerator(SourceProductionContext context, CustomProvider provider)
    {
        //const string INTERFACE_NAME = "IShaderNode";
        const string INTERFACE_VAR_NAME = "ShaderMaterial";

        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(ShaderNodeAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");

        var sb = new StringBuilderSG();
        foreach (var classItem in provider.Classes)
        {
            var classSyntax = classItem.Syntax;
            var classSymbol = classItem.Symbol;

            // Get Attribute Informations
            bool hasAttr = false;

            var arg_chacheShaderMaterialProp = false;
            var arg_chacheShaderScript = false;
            var arg_shaderMaterialPropName = "";
            var arg_shaderScriptPath = "";
            var arg_isVisualShader = false;

            foreach (var attr in classSymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttrSymbol))
                    continue;

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
                        case nameof(ShaderNodeAttribute.shaderMaterialPropName): arg_shaderMaterialPropName = (string)value; break;
                        case nameof(ShaderNodeAttribute.shaderScriptPath): arg_shaderScriptPath = (string)value; break;
                        case nameof(ShaderNodeAttribute.isVisualShader): arg_isVisualShader = (bool)value; break;
                    }
                }

                hasAttr = true;
                break;
            }

            if (!hasAttr)
                continue;

            //check if class is valid node
            if (!classSymbol.IsOfBaseType(nodeSymbol))
            {
                context.NewDiagnostic(classSyntax.GetLocation(), 1, $"The class dont cant have a shader material");
                continue;
            }

            var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var ns = classSymbol.ContainingNamespace.Name;
            if (!string.IsNullOrWhiteSpace(ns))
                sb.AddNamespaceFileScoped(ns);

            var closeClass = sb.CreateBracketDeclaration($"partial class {className}");

            string filePath = string.IsNullOrWhiteSpace(arg_shaderScriptPath)
                ? GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath)
                : arg_shaderScriptPath;

            filePath = Path.ChangeExtension(filePath, arg_isVisualShader ? ".tres" : ".gdshader");

            var scriptType = GodotUtil.GD_G_NAMESPACE + (arg_isVisualShader ? ".Shader" : ".VisualShader");
            if (arg_chacheShaderScript)
            {
                sb.AppendLineC($"public static {scriptType} ShaderScript {{ get; }} = {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }
            else
            {
                sb.AppendLineC($"public static {scriptType} GetShaderScript() => {GD_LOADER}.Load<{scriptType}>(\"{filePath}\")");
            }

            sb.AppendLine();

            const string GD_SM = GodotUtil.GD_G_NAMESPACE + ".ShaderMaterial";
            if (arg_chacheShaderMaterialProp)
            {
                var memberName = UtilString.FirstCharToLowerCase(INTERFACE_VAR_NAME);
                sb.AppendLine($"protected {GD_SM} {memberName}");
                sb.AppendLine($"public {GD_SM} {INTERFACE_VAR_NAME} => {memberName}");
            }
            else
            {
                sb.AppendLineC($"public {GD_SM} {INTERFACE_VAR_NAME} => ({GD_SM}){arg_shaderMaterialPropName}");
            }

            sb.AppendLine();
            closeClass();

            context.AddSource(className + ".g.cs", sb.ToString());
            sb.Clear();
        }

        //static void CreateInterface
        //{
        //    sb.AddNamespaceFileScoped(GodotUtil.GD_NAMESPACE);
        //    //sb.AddNamespaceFileScoped(typeof(ShaderNodeAttribute).Namespace);

        //    var closeClass = sb.CreateBracketDeclaration($"public interface {INTERFACE_NAME}");
        //    sb.AppendLine($"{GodotUtil.GD_G_NAMESPACE}.ShaderMaterial {INTERFACE_VAR_NAME} {{ get; }}");
        //    closeClass();

        //    context.AddSource(INTERFACE_NAME + ".g.cs", sb.ToString());
        //}

    }


}
