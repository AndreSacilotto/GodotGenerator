using Microsoft.CodeAnalysis;

namespace Generator.Generator;

public partial class ProjectDotGodotGenerator
{
    private static void InputMapping(ref SourceProductionContext context, ref int i, string[] content)
    {
        var sb = new StringBuilderSG();

        var closeNS = sb.CreateNamespace(GODOT_NAMESPACE);

        var className = nameof(InputMapping);

        var closeClass = sb.CreateBracketDeclaration($"public static class {className}");

        // Custom Inputs
        for (i++; i < content.Length; i++)
        {
            var line = content[i].AsSpan();

            if (IsSection(line))
            {
                i--;
                break;
            }

            var stop = line.IndexOf('=');
            if (stop == -1)
                continue;

            // Hacky Fast Check to see if is a default one
            if (line[0] == 'u' && line[1] == 'i' && line[2] == '_')
                continue;

            var inputName = line.Slice(0, stop).ToString();

            sb.AppendLineC($"public const string {inputName.ToUpperInvariant()} = \"{inputName}\"");
        }

        // Default Inputs
        sb.AppendLine("#region Default Inputs");
        foreach (var input in defaultInputs)
            sb.AppendLineC($"public const string {input.Replace('.', '_').ToUpperInvariant()} = \"{input}\"");
        sb.AppendLine("#endregion Default Inputs");

        closeClass();
        closeNS();

        context.AddSource($"{className}.g.cs", sb.ToString());
    }

    /// <summary>
    /// https://github.com/godotengine/godot/blob/master/core/input/input_map.cpp#L280
    /// Regex: .*?("".*?"").* | sub: $1,
    /// </summary>
    private static readonly string[] defaultInputs = new string[]
    {
        "ui_accept",
        "ui_select",
        "ui_cancel",
        "ui_focus_next",
        "ui_focus_prev",
        "ui_left",
        "ui_right",
        "ui_up",
        "ui_down",
        "ui_page_up",
        "ui_page_down",
        "ui_home",
        "ui_end",
        "ui_cut",
        "ui_copy",
        "ui_paste",
        "ui_undo",
        "ui_redo",
        "ui_text_completion_query",
        "ui_text_newline",
        "ui_text_newline_blank",
        "ui_text_newline_above",
        "ui_text_indent",
        "ui_text_dedent",
        "ui_text_backspace",
        "ui_text_backspace_word",
        "ui_text_backspace_word.macos",
        "ui_text_backspace_all_to_left",
        "ui_text_backspace_all_to_left.macos",
        "ui_text_delete",
        "ui_text_delete_word",
        "ui_text_delete_word.macos",
        "ui_text_delete_all_to_right",
        "ui_text_delete_all_to_right.macos",
        "ui_text_caret_left",
        "ui_text_caret_word_left",
        "ui_text_caret_word_left.macos",
        "ui_text_caret_right",
        "ui_text_caret_word_right",
        "ui_text_caret_word_right.macos",
        "ui_text_caret_up",
        "ui_text_caret_down",
        "ui_text_caret_line_start",
        "ui_text_caret_line_start.macos",
        "ui_text_caret_line_end",
        "ui_text_caret_line_end.macos",
        "ui_text_caret_page_up",
        "ui_text_caret_page_down",
        "ui_text_caret_document_start",
        "ui_text_caret_document_start.macos",
        "ui_text_caret_document_end",
        "ui_text_caret_document_end.macos",
        "ui_text_caret_add_below",
        "ui_text_caret_add_below.macos",
        "ui_text_caret_add_above",
        "ui_text_caret_add_above.macos",
        "ui_text_scroll_up",
        "ui_text_scroll_up.macos",
        "ui_text_scroll_down",
        "ui_text_scroll_down.macos",
        "ui_text_select_all",
        "ui_text_select_word_under_caret",
        "ui_text_add_selection_for_next_occurrence",
        "ui_text_clear_carets_and_selection",
        "ui_text_toggle_insert_mode",
        "ui_text_submit",
        "ui_graph_duplicate",
        "ui_graph_delete",
        "ui_filedialog_up_one_level",
        "ui_filedialog_refresh",
        "ui_filedialog_show_hidden",
        "ui_swap_input_direction",
    };

}
