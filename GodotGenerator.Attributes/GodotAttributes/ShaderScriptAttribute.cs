﻿namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ShaderScriptAttribute : Attribute
{
    public readonly bool chacheShaderMaterialProp;
    public readonly bool chacheShaderScript;
    public readonly string materialMemberName;
    public readonly string shaderScriptPath;
    public readonly bool isVisualShader;

    public ShaderScriptAttribute(string shaderScriptPath, bool chacheShaderMaterialProp = false, bool chacheShaderScript = false, string materialMemberName = "Material") :
        this(chacheShaderMaterialProp, chacheShaderScript, materialMemberName, shaderScriptPath, false)
    { }

    public ShaderScriptAttribute(bool chacheShaderMaterialProp = false, bool chacheShaderScript = false, string materialMemberName = "Material", bool isVisualShader = false) :
        this(chacheShaderMaterialProp, chacheShaderScript, materialMemberName, string.Empty, isVisualShader)
    { }

    private ShaderScriptAttribute(bool chacheShaderMaterialProp, bool chacheShaderScript, string materialMemberName, string shaderScriptPath, bool isVisualShader)
    {
        this.chacheShaderMaterialProp = chacheShaderMaterialProp;
        this.chacheShaderScript = chacheShaderScript;
        this.materialMemberName = materialMemberName;
        this.shaderScriptPath = shaderScriptPath;
        this.isVisualShader = isVisualShader;
    }
}
