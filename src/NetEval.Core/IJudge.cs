namespace NetEval;

/// <summary>
/// Scores a model output against natural-language criteria.
/// Implementations wrap an LLM (see NetEval.Judges.Claude) or a heuristic.
/// </summary>
public interface IJudge
{
    Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default);
}

/// <param name="Output">The text produced by the system under test.</param>
/// <param name="Criteria">Natural-language criteria the output must satisfy.</param>
/// <param name="Input">Optional: the input that produced the output, for context.</param>
/// <param name="Expected">Optional: a reference answer the judge may compare against.</param>
public sealed record JudgeRequest(string Output, string Criteria, string? Input = null, string? Expected = null);

/// <param name="Passed">Whether the output satisfies the criteria.</param>
/// <param name="Score">0.0–1.0 confidence/quality score.</param>
/// <param name="Reasoning">The judge's explanation, surfaced in test failure messages.</param>
/// <param name="Usage">Judge token usage for cost reporting, when the judge reports it.</param>
public sealed record JudgeVerdict(bool Passed, double Score, string Reasoning, JudgeUsage? Usage = null);
