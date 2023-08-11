using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ShaderNodeAttribute : Attribute
{
    public readonly bool chacheShaderMaterialProp;
    public readonly bool chacheShaderScript;
    public readonly string shaderScriptPath;
    public readonly bool visualShader;

    public ShaderNodeAttribute(bool chacheShaderMaterialProp = false, bool chacheShaderScript = false, string shaderScriptPath = "", bool visualShader = false)
    {
        this.chacheShaderMaterialProp = chacheShaderMaterialProp;
        this.chacheShaderScript = chacheShaderScript;
        this.shaderScriptPath = shaderScriptPath;
        this.visualShader = visualShader;
    }
}
