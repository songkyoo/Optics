namespace Macaron.Optics.Generator;

internal sealed record AnalysisSuccess<TContext>(
    TContext Context
) : AnalysisResult<TContext>;
