using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NetEval;

/// <summary>
/// Verifies an eval run against a baseline snapshot committed to the repo, failing the
/// test when quality regresses beyond tolerance:
/// <code>report.VerifyBaseline("support_bot.baseline.json");</code>
/// A missing baseline is created on first run (commit it). To accept new results after
/// an intentional change, set <c>NETEVAL_UPDATE_BASELINES=1</c> and re-run.
/// </summary>
public static class Baseline
{
    public const string UpdateEnvVar = "NETEVAL_UPDATE_BASELINES";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Compares the report against the baseline at <paramref name="baselinePath"/> and throws
    /// <see cref="BaselineRegressionException"/> on regression. Relative paths resolve against
    /// the calling source file's directory, so baselines live next to the test and get committed.
    /// </summary>
    public static void VerifyBaseline(
        this EvalReport report,
        string baselinePath,
        BaselineOptions? options = null,
        [CallerFilePath] string callerFilePath = "")
    {
        options ??= new BaselineOptions();
        var path = ResolvePath(baselinePath, callerFilePath);

        if (UpdateRequested())
        {
            Write(path, report);
            return;
        }

        if (!File.Exists(path))
        {
            if (options.FailIfMissing)
                throw new BaselineRegressionException(
                    $"No baseline found at '{path}'. Run the evals locally and commit the generated baseline file.");

            Write(path, report);
            return;
        }

        var baseline = Read(path);
        var problems = new List<string>();

        if (report.PassRate < baseline.PassRate - options.PassRateTolerance)
            problems.Add($"pass rate regressed: {baseline.PassRate:P1} ({baseline.Passed}/{baseline.Total}) -> {report.PassRate:P1} ({report.Passed}/{report.Total}), tolerance {options.PassRateTolerance:P1}");

        if (options.MeanScoreTolerance is { } scoreTolerance && report.MeanScore < baseline.MeanScore - scoreTolerance)
            problems.Add($"mean score regressed: {baseline.MeanScore:0.00} -> {report.MeanScore:0.00}, tolerance {scoreTolerance:0.00}");

        if (problems.Count == 0)
            return;

        var previouslyPassing = baseline.Cases.Where(c => c.Passed).Select(c => c.Input).ToHashSet();
        var newlyFailing = report.Results
            .Where(r => !r.Verdict.Passed && previouslyPassing.Contains(r.Case.Input))
            .Select(r => $"newly failing: \"{Truncate(r.Case.Input)}\" — score {r.Verdict.Score:0.00}: {r.Verdict.Reasoning}");

        throw new BaselineRegressionException(
            $"Eval run regressed against baseline '{path}':" + Environment.NewLine +
            string.Join(Environment.NewLine, problems.Concat(newlyFailing).Select(line => "  " + line)) + Environment.NewLine +
            $"If the new results are intentional, set {UpdateEnvVar}=1 and re-run to update the baseline.");
    }

    private static bool UpdateRequested() =>
        Environment.GetEnvironmentVariable(UpdateEnvVar)?.ToLowerInvariant() is "1" or "true";

    private static string ResolvePath(string baselinePath, string callerFilePath)
    {
        if (Path.IsPathRooted(baselinePath))
            return baselinePath;

        var callerDirectory = Path.GetDirectoryName(callerFilePath);
        return string.IsNullOrEmpty(callerDirectory)
            ? Path.GetFullPath(baselinePath)
            : Path.Combine(callerDirectory, baselinePath);
    }

    private static void Write(string path, EvalReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var file = new BaselineFile(
            DateTimeOffset.UtcNow, report.Total, report.Passed, report.PassRate, report.MeanScore,
            report.Results.Select(r => new BaselineCase(r.Case.Input, r.Verdict.Passed, r.Verdict.Score)).ToList());
        File.WriteAllText(path, JsonSerializer.Serialize(file, JsonOptions));
    }

    private static BaselineFile Read(string path) =>
        JsonSerializer.Deserialize<BaselineFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new FormatException($"'{path}' is not a valid baseline file.");

    private static string Truncate(string text) =>
        text.Length <= 60 ? text : text[..60] + "…";

    private sealed record BaselineFile(
        DateTimeOffset CreatedAt, int Total, int Passed, double PassRate, double MeanScore, List<BaselineCase> Cases);

    private sealed record BaselineCase(string Input, bool Passed, double Score);
}

/// <summary>Tuning for <see cref="Baseline.VerifyBaseline"/>.</summary>
public sealed record BaselineOptions
{
    /// <summary>Allowed absolute drop in pass rate before failing. Default 0 — any drop fails.</summary>
    public double PassRateTolerance { get; init; } = 0.0;

    /// <summary>Allowed absolute drop in mean score. Default null — mean score is not gated.</summary>
    public double? MeanScoreTolerance { get; init; }

    /// <summary>
    /// Fail instead of silently creating a missing baseline. Defaults to true when the CI
    /// environment variable is set, so a forgotten baseline can't pass forever on CI.
    /// </summary>
    public bool FailIfMissing { get; init; } = Environment.GetEnvironmentVariable("CI") is not (null or "");
}
