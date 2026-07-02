using NetEval.Xunit;
using Xunit;

namespace NetEval.Core.Tests;

public class LlmFactRetryTests
{
    private static int _flakyCalls;

    // "PATH" is always set, so this doesn't skip; the first attempt deliberately fails.
    // With Runs = 3 and PassThreshold = 0.5 (2 passes required), attempts 2 and 3 pass
    // and the test goes green — proving the retry runner works end to end.
    [LlmFact("PATH", Runs = 3, PassThreshold = 0.5)]
    public void Flaky_test_passes_when_pass_rate_meets_threshold()
    {
        var call = Interlocked.Increment(ref _flakyCalls);
        Assert.True(call > 1, "deliberate first-attempt failure (the retry runner should absorb this)");
    }

    [Theory]
    [InlineData(5, 1.0, 5)]   // default: every run must pass
    [InlineData(5, 0.8, 4)]
    [InlineData(5, 0.6, 3)]
    [InlineData(3, 0.5, 2)]   // 1.5 rounds up
    [InlineData(1, 1.0, 1)]
    [InlineData(5, 0.0, 1)]   // a test that passes zero times can never be meaningful
    [InlineData(5, 7.5, 5)]   // out-of-range threshold clamps to 1.0
    public void RequiredPasses_rounds_up_and_clamps(int runs, double threshold, int expected) =>
        Assert.Equal(expected, LlmFactTestCase.RequiredPasses(runs, threshold));
}
