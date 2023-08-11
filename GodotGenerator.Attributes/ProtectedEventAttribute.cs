using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ProtectedEventAttribute : Attribute
{
    public readonly bool nullable;
    public readonly string eventName;
    public ProtectedEventAttribute(bool nullable = true, string eventName = "")
    {
        this.nullable = nullable;
        this.eventName = eventName;
    }
}
