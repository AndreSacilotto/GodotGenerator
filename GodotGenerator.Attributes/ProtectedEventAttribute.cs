using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class ProtectedEventAttribute : Attribute
{
    internal readonly bool nullable;
    internal readonly string eventName;
    public ProtectedEventAttribute(bool nullable = true, string eventName = "")
    {
        this.nullable = nullable;
        this.eventName = eventName;
    }
}
