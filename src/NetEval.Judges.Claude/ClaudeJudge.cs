namespace NetEval.Judges.Claude;

/// <summary>
/// LLM judge backed by the official Anthropic C# SDK (the "Anthropic" NuGet package).
///
/// Week-1 implementation plan:
///  1. Construct the Anthropic client (reads ANTHROPIC_API_KEY from the environment).
///  2. Either call the Messages API directly with a structured-output verdict schema,
///     or use the SDK's Microsoft.Extensions.AI adapter and delegate to ChatClientJudge.
///  3. Capture token usage from the response for cost reporting.
/// </summary>
public sealed class ClaudeJudge : IJudge
{
    public const string DefaultModel = "claude-opus-4-8";

    private readonly string _model;

    public ClaudeJudge(string model = DefaultModel) => _model = model;

    public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            $"Week 1: wire up the Anthropic SDK here (model: {_model}). See class docs for the plan.");
}
