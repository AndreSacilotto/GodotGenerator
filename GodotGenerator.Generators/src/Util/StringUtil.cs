using System.Text.RegularExpressions;

namespace Generator;

internal static class StringUtil
{
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
        return char.ToLower(input[0]) + input.Substring(1);
    }


    public static string RemoveNamespace(string typeFullName) => Regex.Replace(typeFullName, "[.\\w]+\\.(\\w+)", "$1");
    public static string TypeSimpleName(string typeName)
    {
        var splits = typeName.Split('.');
        var last = splits[splits.Length - 1];

        return last;
    }
}
