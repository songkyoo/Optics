using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor OpticsTargetTypeCannotBeNullableRule = new(
        id: "MOPT0001",
        title: "Optics target type cannot be nullable",
        messageFormat: "Type '{0}' is nullable. Nullable types are not supported as optics targets.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor OpticsTargetTypeMustSupportWithExpressionRule = new(
        id: "MOPT0002",
        title: "Optics target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used as an optics target",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor OpticsAttributeMustBeOnStaticClassRule = new(
        id: "MOPT0003",
        title: "Optics attribute must be applied to a static class",
        messageFormat: "Class '{0}' must be static to use optics attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor OpticsAttributeTargetMustSupportWithExpressionRule = new(
        id: "MOPT0004",
        title: "Optics attribute target type must support 'with' expression",
        messageFormat: "Type '{0}' must be a record or struct to be used with optics attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor OpticsAttributeTargetMustBeSpecifiedRule = new(
        id: "MOPT0005",
        title: "Target type must be specified for optics attribute",
        messageFormat: "Class '{0}' is not nested in a target type. Specify the target type explicitly.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
