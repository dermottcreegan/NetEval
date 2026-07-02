using System.Text;

namespace NetEval;

/// <param name="Case">The dataset row.</param>
/// <param name="Output">What the system under test produced.</param>
/// <param name="Verdict">The judge's scoring of the output.</param>
/// <param name="SutLatency">Time the system under test took — the latency users would see.</param>
/// <param name="JudgeLatency">Time the judge took — eval overhead, not product latency.</param>
public sealed record EvalCaseResult(
    EvalCase Case,
    string Output,
    JudgeVerdict Verdict,
    TimeSpan SutLatency,
    TimeSpan JudgeLatency);

/// <summary>Aggregate results of one dataset eval run. <see cref="ToString"/> prints a summary.</summary>
public sealed class EvalReport(IReadOnlyList<EvalCaseResult> results)
{
    public IReadOnlyList<EvalCaseResult> Results { get; } = results;

    public int Total => Results.Count;
    public int Passed => Results.Count(r => r.Verdict.Passed);
    public int Failed => Total - Passed;
    public double PassRate => Total == 0 ? 0.0 : (double)Passed / Total;
    public double MeanScore => Total == 0 ? 0.0 : Results.Average(r => r.Verdict.Score);
    public TimeSpan MeanSutLatency =>
        Total == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)Results.Average(r => r.SutLatency.Ticks));

    /// <summary>Judge token usage summed across all cases (zeros when the judge reports none).</summary>
    public JudgeUsage JudgeUsage =>
        Results.Aggregate(JudgeUsage.Zero, (total, r) => total + (r.Verdict.Usage ?? JudgeUsage.Zero));

    public IEnumerable<EvalCaseResult> Failures => Results.Where(r => !r.Verdict.Passed);

    public override string ToString()
    {
        var summary = new StringBuilder()
            .AppendLine($"Eval run: {Passed}/{Total} passed ({PassRate:P0}), mean score {MeanScore:0.00}")
            .AppendLine($"Mean SUT latency: {MeanSutLatency.TotalMilliseconds:0} ms; judge usage: {JudgeUsage.InputTokens} in / {JudgeUsage.OutputTokens} out tokens");

        foreach (var (failure, number) in Results.Select((r, i) => (r, i + 1)).Where(x => !x.r.Verdict.Passed))
            summary.AppendLine($"  FAIL case #{number} \"{Truncate(failure.Case.Input, 60)}\" — score {failure.Verdict.Score:0.00}: {failure.Verdict.Reasoning}");

        return summary.ToString().TrimEnd();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
