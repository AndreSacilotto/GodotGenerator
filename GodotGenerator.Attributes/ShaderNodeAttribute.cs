using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ShaderNodeAttribute : Attribute
{
    public readonly bool chacheShaderMaterialProp;
    public readonly bool chacheShaderScript;
    public readonly string shaderMaterialPropName;
    public readonly string shaderScriptPath;
    public readonly bool isVisualShader;
    public ShaderNodeAttribute(bool chacheShaderMaterialProp = false, bool chacheShaderScript = false, string shaderMaterialPropName = "Material", string shaderScriptPath = "", bool isVisualShader = false)
    {
        this.chacheShaderMaterialProp = chacheShaderMaterialProp;
        this.chacheShaderScript = chacheShaderScript;
        this.shaderMaterialPropName = shaderMaterialPropName;
        this.shaderScriptPath = shaderScriptPath;
        this.isVisualShader = isVisualShader;
    }
}
