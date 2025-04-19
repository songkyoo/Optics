namespace Macaron.Optics;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class OptionalOfAttribute : Attribute
{
    public Type? TargetType { get; }

    public OptionalOfAttribute()
    {
        TargetType = null;
    }

    public OptionalOfAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
