using System;

namespace Jerry;


[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class ImplementedBy : Attribute
{
    public ImplementedBy(Type implementation)
    {
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class Descriptor : Attribute
{
    public Descriptor(Type implementation)
    {
    }
}