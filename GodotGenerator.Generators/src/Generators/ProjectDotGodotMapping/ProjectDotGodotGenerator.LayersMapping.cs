using Microsoft.CodeAnalysis;

namespace Generator.Generators;

partial class ProjectDotGodotGenerator
{
    private record struct LayerValue(string LayerName, string[] LayersName);

    private static void LayersMapping(ref SourceProductionContext context, ReadOnlySpan<string> content)
    {
        var sb = new StringBuilderSG();

        var className = nameof(LayersMapping);

        sb.AddNamespaceFileScoped(GodotUtil.GD_NAMESPACE);

        var closeClass = sb.CreateBracketDeclaration($"public static class {className}");

        var layersDict = new Dictionary<string, LayerValue>
        {
            ["2d_render"] = new("Render2D", new string[20]),
            ["2d_physics"] = new("Physics2D", new string[32]),
            ["2d_navigation"] = new("Navigation2D", new string[32]),

            ["3d_render"] = new("Render3D", new string[20]),
            ["3d_physics"] = new("Physics3D", new string[32]),
            ["3d_navigation"] = new("Navigation3D", new string[32]),

            ["avoidance"] = new("Avoidance", new string[32]),
        };

        for (int i = 0; i < content.Length; i++)
        {
            var line = content[i].AsSpan();

            // Parsing ( 2d_render/layer_1="Water" )
            int start = 0;

            // '2d_render'
            var i1 = line.IndexOf('/');
            var layerString = line.Slice(start, i1).ToString();

            if (!layersDict.TryGetValue(layerString, out var arr))
                continue; // TODO: ERROR

            // 'Water'
            var i2 = line.IndexOf('=');
            start = i2 + 2;
            var nameString = line.Slice(start, line.Length - start - 1).ToString();

            // 'layer_1' => -'layer_'(6) => '1'
            start = i1 + 1 + 6;
            var numberString = line.Slice(start, i2 - start).ToString();

            arr.LayersName[int.Parse(numberString)-1] = nameString;
        }

        foreach (var layer in layersDict.Values)
        {
            sb.AddAttribute("System.Flags");
            var closeEnum = sb.CreateBracketDeclaration($"public enum {layer.LayerName}");
            sb.AppendLine($"None = 0,");

            var arr = layer.LayersName;
            for (int i = 0; i < arr.Length; i++)
            {
                var item = arr[i];
                if (string.IsNullOrEmpty(item))
                    item = "Layer" + (i+1);
                sb.AppendLine($"{item} = 1 << {i},");
            }

            closeEnum();
        }

        closeClass();

        context.AddSource($"{className}.g.cs", sb.ToString());
    }

}
