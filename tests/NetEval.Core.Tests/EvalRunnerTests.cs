using NetEval;
using Xunit;

namespace NetEval.Core.Tests;

public class EvalRunnerTests
{
    /// <summary>Passes any output containing "ok"; reports fixed token usage per call.</summary>
    private sealed class ContainsOkJudge : IJudge
    {
        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
        {
            var passed = request.Output.Contains("ok");
            return Task.FromResult(new JudgeVerdict(passed, passed ? 1.0 : 0.0, "scripted", new JudgeUsage(100, 10)));
        }
    }

    private static readonly IReadOnlyList<EvalCase> Cases =
    [
        new EvalCase("greet", Criteria: "any"),
        new EvalCase("insult", Criteria: "any"),
        new EvalCase("farewell", Criteria: "any"),
    ];

    private static Task<string> FakeSut(string input) =>
        Task.FromResult(input == "insult" ? "bad reply" : $"ok: {input}");

    [Fact]
    public async Task Aggregates_pass_rate_scores_and_usage()
    {
        var report = await new EvalRunner(new ContainsOkJudge()).RunAsync(Cases, FakeSut);

        Assert.Equal(3, report.Total);
        Assert.Equal(2, report.Passed);
        Assert.Equal(1, report.Failed);
        Assert.Equal(2.0 / 3.0, report.PassRate, precision: 10);
        Assert.Equal(new JudgeUsage(300, 30), report.JudgeUsage);
        Assert.Single(report.Failures);
    }

    [Fact]
    public async Task Case_criteria_fall_back_to_default_criteria()
    {
        var seenCriteria = new List<string>();
        var spyJudge = new SpyJudge(seenCriteria);
        var cases = new[] { new EvalCase("a"), new EvalCase("b", Criteria: "case-specific") };

        await new EvalRunner(spyJudge).RunAsync(cases, FakeSut, defaultCriteria: "the default");

        Assert.Equal(["the default", "case-specific"], seenCriteria);
    }

    [Fact]
    public async Task Missing_criteria_throws_with_case_number()
    {
        var cases = new[] { new EvalCase("only input, no criteria") };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new EvalRunner(new ContainsOkJudge()).RunAsync(cases, FakeSut));

        Assert.Contains("Case #1", ex.Message);
    }

    [Fact]
    public async Task Expected_answer_is_passed_to_the_judge()
    {
        JudgeRequest? seen = null;
        var judge = new LambdaJudge(r => { seen = r; return new JudgeVerdict(true, 1.0, ""); });
        var cases = new[] { new EvalCase("q", Criteria: "any", Expected: "the reference answer") };

        await new EvalRunner(judge).RunAsync(cases, FakeSut);

        Assert.Equal("the reference answer", seen!.Expected);
    }

    [Fact]
    public async Task Report_summary_lists_failures_with_reasoning()
    {
        var report = await new EvalRunner(new ContainsOkJudge()).RunAsync(Cases, FakeSut);
        var summary = report.ToString();

        Assert.Contains("2/3 passed", summary);
        Assert.Contains("FAIL case #2", summary);
        Assert.Contains("scripted", summary);
        Assert.Contains("300 in / 30 out tokens", summary);
    }

    [Fact]
    public void Usage_cost_is_computed_from_per_million_token_prices()
    {
        var usage = new JudgeUsage(InputTokens: 2_000_000, OutputTokens: 1_000_000);
        Assert.Equal(2 * 5.0 + 1 * 25.0, usage.CostUsd(inputPricePerMTok: 5.0, outputPricePerMTok: 25.0), precision: 10);
    }

    private sealed class SpyJudge(List<string> seenCriteria) : IJudge
    {
        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
        {
            seenCriteria.Add(request.Criteria);
            return Task.FromResult(new JudgeVerdict(true, 1.0, ""));
        }
    }

    private sealed class LambdaJudge(Func<JudgeRequest, JudgeVerdict> judge) : IJudge
    {
        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(judge(request));
    }
}
