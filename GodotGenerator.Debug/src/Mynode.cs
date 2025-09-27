using Generator.Attributes;
using Godot;
using System;

namespace SourceGenDebug;

[WhatNotification(WhatNotificationAttribute.BaseCall.After)]
[SceneScript()]
public partial class Mynode : Node
{
    [WhatNotificationMethod(100)]
    public void Kill()
    {
        GD.Print(nameof(Kill));
    }

    [WhatNotificationMethod((int)NotificationProcess)]
    public void Delta()
    {
        GD.Print((float)GetProcessDeltaTime());
    }

}
