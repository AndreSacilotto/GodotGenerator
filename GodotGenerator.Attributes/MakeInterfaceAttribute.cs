﻿namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MakeInterfaceAttribute : Attribute
{
    public readonly bool useProps;
    public readonly bool useMethods;
    public readonly bool useEvents;

    /// <summary>inherit interfaces that the class already have</summary>
    public readonly bool inheritInterfaces;

    /// <summary>inherit interfaces that are generated by MakeInterface source generator</summary>
    public readonly bool inheritGeneratedInterfaces;

    public MakeInterfaceAttribute(bool useProps = true, bool useMethods = false, bool useEvents = false, bool inheritInterfaces = false, bool inheritGeneratedInterfaces = true)
    {
        this.useProps = useProps;
        this.useMethods = useMethods;
        this.useEvents = useEvents;
        this.inheritInterfaces = inheritInterfaces;
        this.inheritGeneratedInterfaces = inheritGeneratedInterfaces;
    }
}
