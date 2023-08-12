using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScriptSceneAttribute : Attribute
{
    public readonly bool chachePackedScene;
    public readonly string scenePath;
    //public readonly bool createEmptyConstructor;
    public ScriptSceneAttribute(bool chachePackedScene = false, string scenePath = "")
    {
        this.chachePackedScene = chachePackedScene;
        this.scenePath = scenePath;
    }
}
