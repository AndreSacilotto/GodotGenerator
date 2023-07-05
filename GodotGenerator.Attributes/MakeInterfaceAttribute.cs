﻿using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MakeInterfaceAttribute : Attribute
{
    internal readonly bool useProps;
    internal readonly bool useMethods;
    internal readonly bool useEvents;

    /// <summary>inherit interfaces that the class already have</summary>
    internal readonly bool inheritInterfaces;

    /// <summary>inherit interfaces that are generated by MakeInterface source generator</summary>
    internal readonly bool inheritGeneratedInterfaces;

    public MakeInterfaceAttribute(bool useProps = true, bool useMethods = false, bool useEvents = false, bool inheritInterfaces = false, bool inheritGeneratedInterfaces = true)
    {
        this.useProps = useProps;
        this.useMethods = useMethods;
        this.useEvents = useEvents;
        this.inheritInterfaces = inheritInterfaces;
        this.inheritGeneratedInterfaces = inheritGeneratedInterfaces;
    }

}
