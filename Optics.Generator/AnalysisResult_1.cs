using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Macaron.Optics.Generator;

internal abstract record AnalysisResult<TContext>
{
    public sealed record Success(TContext Context) : AnalysisResult<TContext>;

    public sealed record Failure(Diagnostic Diagnostic) : AnalysisResult<TContext>;
}
