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


    private static void Execute(SourceProductionContext context, string provider)
    {
        var fileContent = provider.Split(StringUtil.NewLines, StringSplitOptions.RemoveEmptyEntries).AsSpan();
        if (fileContent == null || fileContent.Length == 0)
            return;

        var input = "input".AsSpan();
        var layer_names = "layer_names".AsSpan();

        var customInput = ReadOnlySpan<string>.Empty;
        var customLayer = ReadOnlySpan<string>.Empty;

        for (int i = 0; i < fileContent.Length; i++)
        {
            var line = fileContent[i].AsSpan();
            if (IsSection(line))
            {
                var section = line.Slice(1, line.Length - 2);
                if (section.SequenceEqual(input))
                {
                    var length = LoopUntilEndOfSection(fileContent, i);
                    customInput = fileContent.Slice(i + 1, length);
                    i += length;
                }
                else if (section.SequenceEqual(layer_names))
                {
                    var length = LoopUntilEndOfSection(fileContent, i);
                    customLayer = fileContent.Slice(i + 1, length);
                    i += length;
                }

            }
        }

        InputMapping(ref context, customInput);

        LayersMapping(ref context, customLayer);

        static bool IsSection(ReadOnlySpan<char> line) => line[0] == '[' && line[line.Length - 1] == ']';

        static int LoopUntilEndOfSection(Span<string> content, int i)
        {
            int length = 0;
            for (i++; i < content.Length; i++)
            {
                if (IsSection(content[i].AsSpan()))
                    break;
                length++;
            }
            return length;
        }

    }

}
