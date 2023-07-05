using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ScriptSceneAttribute : Attribute
{
    internal readonly bool chachePackedScene;
    internal readonly string resPath;
    public ScriptSceneAttribute(bool chachePackedScene = false, string resPath = "")
    {
        this.chachePackedScene = chachePackedScene;
        this.resPath = resPath;
    }
}
