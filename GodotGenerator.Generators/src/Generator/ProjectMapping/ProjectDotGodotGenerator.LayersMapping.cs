using Microsoft.CodeAnalysis;

namespace Generator.Generator;

public partial class ProjectDotGodotGenerator
{
    private record LayerValue(string LayerMask, string Name);

    private static void LayersMapping(ref SourceProductionContext context, ref int i, string[] content)
    {
        var sb = new StringBuilderSG();

        var closeNS = sb.CreateNamespace(GODOT_NAMESPACE);

        var className = nameof(LayersMapping);

        //var closeClass = sb.CreateBracketDeclaration($"public static class {className}");

        var dict = new Dictionary<string, List<LayerValue>>(6);

        for (i++; i < content.Length; i++)
        {
            var line = content[i].AsSpan();

            if (IsSection(line))
            {
                i--;
                break;
            }

            // Parse Layers
            var i1 = line.IndexOf('/') + 1;
            var i2 = line.IndexOf('=') + 2;

            var s1 = line.Slice(0, i1 - 1).ToString();
            var s2 = line.Slice(i1, i2 - i1 - 2);
            var s3 = line.Slice(i2, line.Length - i2 - 1).ToString();

            if (!dict.TryGetValue(s1, out var list))
                dict.Add(s1, list = new List<LayerValue>(8));

            // Create Flag/Mask
            var under = s2.IndexOf('_') + 1;
            var lastMask = s2.Slice(under, s2.Length - under).ToString();
            list.Add(new LayerValue(lastMask, s3));
        }

        foreach (var name in dict)
        {
            var key = name.Key.AsSpan();
            var idx = key.IndexOf('_') + 1;

            var layer = key.Slice(idx, key.Length - idx).ToArray();
            layer[0] = char.ToUpperInvariant(layer[0]);

            var dimension = key.Slice(0, idx - 1);

            var enumName = new string(layer) + "Layer" + dimension.ToString().ToUpperInvariant();

            sb.AddAttribute("System.Flags");
            var closeEnum = sb.CreateBracketDeclaration($"public enum {enumName} : uint");
            sb.AppendLine($"None = 0,");

            foreach (var item in name.Value)
                sb.AppendLine($"{item.Name} = 1 << {item.LayerMask},");

            closeEnum();
        }

        //closeClass();
        closeNS();

        context.AddSource($"{className}.g.cs", sb.ToString());
    }

}
