using Xunit;
using Xunit.Sdk;

namespace NetEval.Xunit;

/// <summary>
/// Marks a test that exercises an LLM. Auto-skips when the required API key
/// environment variable is not set, so CI without secrets stays green.
///
/// Set <see cref="Runs"/> and <see cref="PassThreshold"/> to absorb LLM
/// non-determinism: the test runs up to N times and passes when the pass rate
/// meets the threshold. See <see cref="LlmFactTestCase"/> for the semantics.
/// </summary>
[XunitTestCaseDiscoverer("NetEval.Xunit.LlmFactDiscoverer", "NetEval.Xunit")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LlmFactAttribute : FactAttribute
{
    /// <summary>How many times to run the test (threshold semantics; stops early once decided).</summary>
    public int Runs { get; set; } = 1;

    /// <summary>Fraction of runs that must pass, 0.0–1.0. Defaults to 1.0 (every run must pass).</summary>
    public double PassThreshold { get; set; } = 1.0;

    public LlmFactAttribute(string requiredEnvVar = "ANTHROPIC_API_KEY")
    {
        if (Environment.GetEnvironmentVariable(requiredEnvVar) is null or "")
            Skip = $"Skipped: environment variable '{requiredEnvVar}' is not set.";
    }
}
