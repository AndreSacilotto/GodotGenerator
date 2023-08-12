using Microsoft.CodeAnalysis;

namespace Generator.Generators;

[Generator]
internal partial class ProjectDotGodotGenerator : IIncrementalGenerator
{
    //config version = 5
    public const string GODOT_PROJECT_FILE = "project.godot";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var additionalTexts = context.AdditionalTextsProvider.Where(
            static file => file.Path.EndsWith(GODOT_PROJECT_FILE)
        );

        var projectFiles = additionalTexts.Select(
            static (additionalText, cancellationToken) => additionalText.GetText(cancellationToken)!.ToString()
        );

        context.RegisterSourceOutput(projectFiles, Execute);
    }

    private static bool IsSection(ReadOnlySpan<char> line) => line[0] == '[' && line[line.Length - 1] == ']';

    private static void Execute(SourceProductionContext context, string provider)
    {
        var content = provider.Split(GeneratorUtil.NewLines, StringSplitOptions.RemoveEmptyEntries);
        if (content == null || content.Length == 0)
            return;

        var input = "input".AsSpan();
        var layer_names = "layer_names".AsSpan();

        for (int i = 0; i < content.Length; i++)
        {
            var line = content[i].AsSpan();

            if (IsSection(line))
            {
                var section = line.Slice(1, line.Length - 2);
                if (section.Equals(input, StringComparison.Ordinal))
                {
                    InputMapping(ref context, ref i, content);
                }
                else if (section.Equals(layer_names, StringComparison.Ordinal))
                {
                    LayersMapping(ref context, ref i, content);
                }
            }
        }

    }

}
