using System.Diagnostics;

namespace NetEval;

/// <summary>
/// Runs a dataset of eval cases through a system under test and judges every output:
/// <code>var report = await new EvalRunner(judge).RunAsync(cases, bot.Reply);</code>
/// </summary>
public sealed class EvalRunner(IJudge? judge = null)
{
    public Task<EvalReport> RunAsync(
        IReadOnlyList<EvalCase> cases,
        Func<string, Task<string>> systemUnderTest,
        string? defaultCriteria = null,
        CancellationToken cancellationToken = default)
        => RunAsync(cases, (input, _) => systemUnderTest(input), defaultCriteria, cancellationToken);

    public async Task<EvalReport> RunAsync(
        IReadOnlyList<EvalCase> cases,
        Func<string, CancellationToken, Task<string>> systemUnderTest,
        string? defaultCriteria = null,
        CancellationToken cancellationToken = default)
    {
        var activeJudge = judge ?? NetEvalConfig.DefaultJudge
            ?? throw new InvalidOperationException(
                "No judge configured. Pass one to the EvalRunner constructor or set NetEvalConfig.DefaultJudge.");

        var results = new List<EvalCaseResult>(cases.Count);
        foreach (var (evalCase, number) in cases.Select((c, i) => (c, i + 1)))
        {
            var criteria = evalCase.Criteria ?? defaultCriteria
                ?? throw new InvalidOperationException(
                    $"Case #{number} has no criteria and no default criteria were given.");

            var sutTimer = Stopwatch.StartNew();
            var output = await systemUnderTest(evalCase.Input, cancellationToken);
            sutTimer.Stop();

            var judgeTimer = Stopwatch.StartNew();
            var verdict = await activeJudge.JudgeAsync(
                new JudgeRequest(output, criteria, evalCase.Input, evalCase.Expected), cancellationToken);
            judgeTimer.Stop();

            results.Add(new EvalCaseResult(evalCase, output, verdict, sutTimer.Elapsed, judgeTimer.Elapsed));
        }

        return new EvalReport(results);
    }
}
