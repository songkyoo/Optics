using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal sealed record AnalysisFailure<TContext>(
    Diagnostic Diagnostic
) : AnalysisResult<TContext>;
