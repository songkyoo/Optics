namespace Macaron.Optics;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class LensOfAttribute : Attribute
{
    public Type? TargetType { get; }

    public LensOfAttribute()
    {
        TargetType = null;
    }

    public LensOfAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
