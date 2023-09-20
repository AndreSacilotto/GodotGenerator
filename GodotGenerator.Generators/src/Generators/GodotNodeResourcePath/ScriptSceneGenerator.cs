using Microsoft.CodeAnalysis;

namespace Generator.Generators;

partial class GodotNodeResourcePathGenerator
{
    public static void ScriptSceneGenerator(SourceProductionContext context, CustomProvider provider)
    {
        const string GD_PACKED = GodotUtil.GD_G_NAMESPACE + ".PackedScene";
        const string GD_LOADER = GodotUtil.GD_G_NAMESPACE + ".ResourceLoader";

        var markerAttrSymbol = provider.Compilation.GetSymbolByName(typeof(Attributes.ScriptSceneAttribute).FullName);//"Generator.Attributes.ScriptSceneAttribute");
        var nodeSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Node");
        //var resourceSymbol = provider.Compilation.GetSymbolByName(GodotUtil.GD_NAMESPACE + ".Resource");

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

            string filePath;
            if (string.IsNullOrWhiteSpace(arg_scenePath))
            {
                filePath = GodotUtil.PathToGodotPath(provider.TargetPath, classSyntax.SyntaxTree.FilePath);
                filePath = Path.ChangeExtension(filePath, ".tscn");
            }
            else
                filePath = arg_scenePath;

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

            //sb.AppendLine();
            //sb.AppendLine($"private {className}() {{ }}");

            closeClass();

            //OutputWriter.WriteFileOnProject(sb.ToString(), "Tests\\" + className + ".txt");
            context.AddSource($"{className}.{nameof(ScriptSceneGenerator)}.g.cs", sb.ToString());
            sb.Clear();
        }
    }

}
