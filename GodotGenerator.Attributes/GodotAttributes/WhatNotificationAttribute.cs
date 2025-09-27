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

    public readonly string method;
    public readonly int baseCall;
    public WhatNotificationAttribute(string method = "private void CustomNotification", BaseCall baseCall = BaseCall.NoCall)
    {
        this.method = method;
        this.baseCall = (int)baseCall;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class WhatNotificationMethodAttribute : Attribute
{
    public readonly int what;
    public WhatNotificationMethodAttribute(long what)
    {
        this.what = (int)what;
    }
    public WhatNotificationMethodAttribute(int what)
    {
        this.what = what;
    }
}
