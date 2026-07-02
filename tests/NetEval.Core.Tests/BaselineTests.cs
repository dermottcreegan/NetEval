using NetEval;
using Xunit;

namespace NetEval.Core.Tests;

public class BaselineTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "neteval-tests", Guid.NewGuid().ToString("N"));

    // Explicit so tests behave the same locally and on CI (where FailIfMissing defaults to true).
    private static readonly BaselineOptions CreateIfMissing = new() { FailIfMissing = false };

    private string PathFor(string name) => Path.Combine(_dir, name);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static EvalReport Report(params (string Input, bool Passed, double Score)[] cases) =>
        new(cases.Select(c => new EvalCaseResult(
            new EvalCase(c.Input),
            c.Passed ? "good output" : "bad output",
            new JudgeVerdict(c.Passed, c.Score, c.Passed ? "fine" : "did not meet criteria"),
            TimeSpan.Zero,
            TimeSpan.Zero)).ToList());

    [Fact]
    public void Creates_missing_baseline_then_passes_on_identical_rerun()
    {
        var path = PathFor("new.baseline.json");
        var report = Report(("a", true, 1.0), ("b", false, 0.2));

        report.VerifyBaseline(path, CreateIfMissing);

        Assert.True(File.Exists(path));
        report.VerifyBaseline(path, CreateIfMissing); // same results — no throw
    }

    [Fact]
    public void Missing_baseline_fails_when_FailIfMissing()
    {
        var ex = Assert.Throws<BaselineRegressionException>(() =>
            Report(("a", true, 1.0)).VerifyBaseline(PathFor("absent.json"), new BaselineOptions { FailIfMissing = true }));

        Assert.Contains("No baseline found", ex.Message);
        Assert.False(File.Exists(PathFor("absent.json")));
    }

    [Fact]
    public void Regressed_pass_rate_throws_and_names_newly_failing_cases()
    {
        var path = PathFor("regress.json");
        Report(("a", true, 1.0), ("b", true, 0.9)).VerifyBaseline(path, CreateIfMissing);

        var ex = Assert.Throws<BaselineRegressionException>(() =>
            Report(("a", true, 1.0), ("b", false, 0.3)).VerifyBaseline(path, CreateIfMissing));

        Assert.Contains("pass rate regressed: 100.0", ex.Message);
        Assert.Contains("newly failing: \"b\"", ex.Message);
        Assert.Contains(Baseline.UpdateEnvVar, ex.Message);
    }

    [Fact]
    public void Pass_rate_tolerance_allows_a_bounded_drop()
    {
        var path = PathFor("tolerance.json");
        Report(("a", true, 1.0), ("b", true, 1.0)).VerifyBaseline(path, CreateIfMissing);

        Report(("a", true, 1.0), ("b", false, 0.3)).VerifyBaseline(
            path, new BaselineOptions { PassRateTolerance = 0.5, FailIfMissing = false });
    }

    [Fact]
    public void Improvement_passes_without_touching_the_baseline()
    {
        var path = PathFor("improve.json");
        Report(("a", true, 1.0), ("b", false, 0.2)).VerifyBaseline(path, CreateIfMissing);
        var before = File.ReadAllText(path);

        Report(("a", true, 1.0), ("b", true, 0.9)).VerifyBaseline(path, CreateIfMissing);

        Assert.Equal(before, File.ReadAllText(path));
    }

    [Fact]
    public void Mean_score_is_gated_only_when_a_tolerance_is_set()
    {
        var path = PathFor("score.json");
        Report(("a", true, 1.0), ("b", true, 1.0)).VerifyBaseline(path, CreateIfMissing);
        var slipped = Report(("a", true, 0.6), ("b", true, 0.6)); // same pass rate, lower scores

        slipped.VerifyBaseline(path, CreateIfMissing); // ungated by default

        var ex = Assert.Throws<BaselineRegressionException>(() =>
            slipped.VerifyBaseline(path, new BaselineOptions { MeanScoreTolerance = 0.1, FailIfMissing = false }));
        Assert.Contains("mean score regressed", ex.Message);
    }

    [Fact]
    public void Update_env_var_accepts_regressed_results_and_rewrites_the_baseline()
    {
        var path = PathFor("update.json");
        Report(("a", true, 1.0), ("b", true, 1.0)).VerifyBaseline(path, CreateIfMissing);

        Environment.SetEnvironmentVariable(Baseline.UpdateEnvVar, "1");
        try
        {
            var regressed = Report(("a", true, 1.0), ("b", false, 0.2));
            regressed.VerifyBaseline(path, CreateIfMissing); // no throw

            Environment.SetEnvironmentVariable(Baseline.UpdateEnvVar, null);
            regressed.VerifyBaseline(path, CreateIfMissing); // rewritten baseline now matches
        }
        finally
        {
            Environment.SetEnvironmentVariable(Baseline.UpdateEnvVar, null);
        }
    }
}
