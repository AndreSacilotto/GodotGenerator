namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WhatNotificationAttribute : Attribute
{
    public enum BaseCall : int
    {
        Before = -1,
        NoCall = 0,
        After = 1,
    }

    public readonly int baseCall;
    public WhatNotificationAttribute(BaseCall baseCall = BaseCall.NoCall)
    {
        this.baseCall = (int)baseCall;
    }
    public WhatNotificationAttribute(int baseCall = 0)
    {
        this.baseCall = baseCall;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class WhatNotificationMethodAttribute : Attribute
{
    public readonly int what;
    public WhatNotificationMethodAttribute(int what)
    {
        this.what = what;
    }
}
