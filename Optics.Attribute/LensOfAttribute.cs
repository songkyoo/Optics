using System.Diagnostics;

namespace Macaron.Optics;

[Conditional("SOURCE_GENERATOR_ONLY")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LensOfAttribute(Type? targetType = null) : Attribute
{
    public Type? TargetType { get; } = targetType;
}
