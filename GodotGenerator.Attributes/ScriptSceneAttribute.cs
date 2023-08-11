using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScriptSceneAttribute : Attribute
{
    public readonly bool chachePackedScene;
    public readonly string resourcePath;
    public ScriptSceneAttribute(bool chachePackedScene = false, string resourcePath = "")
    {
        this.chachePackedScene = chachePackedScene;
        this.resourcePath = resourcePath;
    }
}
