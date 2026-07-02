using Xunit;

namespace NetEval.Xunit;

/// <summary>
/// Marks a test that exercises an LLM. Auto-skips when the required API key
/// environment variable is not set, so CI without secrets stays green.
///
/// Week-2 scope (see roadmap): a custom test-case runner that executes the test
/// <see cref="Runs"/> times and passes when the pass rate meets <see cref="PassThreshold"/>,
/// to absorb LLM non-determinism. Until then this behaves as a single-run [Fact].
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LlmFactAttribute : FactAttribute
{
    /// <summary>How many times to run the test (majority/threshold semantics). Not yet enforced.</summary>
    public int Runs { get; set; } = 1;

    /// <summary>Fraction of runs that must pass, 0.0–1.0. Not yet enforced.</summary>
    public double PassThreshold { get; set; } = 1.0;

    public LlmFactAttribute(string requiredEnvVar = "ANTHROPIC_API_KEY")
    {
        if (Environment.GetEnvironmentVariable(requiredEnvVar) is null or "")
            Skip = $"Skipped: environment variable '{requiredEnvVar}' is not set.";
    }
}
