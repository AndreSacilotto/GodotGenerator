namespace Generator;

internal static class StringUtil
{
    public static readonly string[] NewLines = new string[3] { "\r\n", "\r", "\n" };

    public static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int place = source.LastIndexOf(find);
        if (place == -1)
            return source;
        return source.Remove(place, find.Length).Insert(place, replace);
    }

    public static string FirstCharToLowerCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    public static string FirstCharToUpperCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }


    public static string TypeSimpleName(string typeName)
    {
        var splits = typeName.Split('.');
        var last = splits[splits.Length - 1];
        return last;
    }

    public static string SnakeToPascal(string snake)
    {
        var strings = snake.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries).Select(FirstCharToUpperCase);
        return string.Join(string.Empty, strings);
    }

}
