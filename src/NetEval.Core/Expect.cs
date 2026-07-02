namespace NetEval;

/// <summary>
/// Entry point for semantic assertions:
/// <code>await Expect.That(reply).Satisfies("mentions the refund policy");</code>
/// </summary>
public static class Expect
{
    public static ExpectBuilder That(string output) => new(output);
}

public sealed class ExpectBuilder
{
    private readonly string _output;

    internal ExpectBuilder(string output) => _output = output;

    /// <summary>
    /// Asserts that the output satisfies the given natural-language criteria,
    /// as scored by <paramref name="judge"/> (or <see cref="NetEvalConfig.DefaultJudge"/>).
    /// Throws <see cref="EvalFailedException"/> with the judge's reasoning on failure.
    /// </summary>
    public async Task Satisfies(string criteria, IJudge? judge = null, CancellationToken cancellationToken = default)
    {
        judge ??= NetEvalConfig.DefaultJudge
            ?? throw new InvalidOperationException(
                "No judge configured. Pass one explicitly or set NetEvalConfig.DefaultJudge (e.g. new ClaudeJudge()).");

        var verdict = await judge.JudgeAsync(new JudgeRequest(_output, criteria), cancellationToken);

        if (!verdict.Passed)
            throw new EvalFailedException(criteria, _output, verdict);
    }
}
