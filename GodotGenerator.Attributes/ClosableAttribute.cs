//using System;

//namespace Generator.Attributes;

//TODO

//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = false)]
//public sealed class ClosableAttribute : Attribute
//{
//    public readonly string callback;
//    public ClosableAttribute(string callback)
//    {
//        this.callback = callback;
//    }
//}

/* Example
[Closable("CustomClose()")]
public class Test
{
    [Closable("null")]
    public event Action? OnDo;

    public void Close() 
    { 
        CustomClose();
        OnDo = null;
    }
}
*/
