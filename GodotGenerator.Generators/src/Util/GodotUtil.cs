namespace Generator;

internal class GodotUtil
{
    public const string GD_NAMESPACE = "Godot";
    public const string GD_G_NAMESPACE = "global::" + GD_NAMESPACE;

    public static string PathToGodotPath(string projectPath, string absoluteFilePath) =>
        "res://" + absoluteFilePath.Substring(projectPath.Length).Replace('\\', '/').TrimStart('/');
}
