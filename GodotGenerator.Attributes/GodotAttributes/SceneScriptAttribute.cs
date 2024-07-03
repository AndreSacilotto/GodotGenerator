namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SceneScriptAttribute : Attribute
{
    public readonly bool chachePackedScene;
    public readonly string scenePath;
    //public readonly bool CreateEmptyConstructor;
    public SceneScriptAttribute(bool chachePackedScene = false, string scenePath = "")
    {
        this.chachePackedScene = chachePackedScene;
        this.scenePath = scenePath;
    }
}
