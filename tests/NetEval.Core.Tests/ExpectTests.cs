using NetEval;
using Xunit;

namespace NetEval.Core.Tests;

public class ExpectTests
{
    private sealed class StubJudge(JudgeVerdict verdict) : IJudge
    {
        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(verdict);
    }

    [Fact]
    public async Task Passes_when_judge_passes()
    {
        var judge = new StubJudge(new JudgeVerdict(true, 1.0, "ok"));

        await Expect.That("some output").Satisfies("some criteria", judge);
    }

    [Fact]
    public async Task Throws_with_reasoning_when_judge_fails()
    {
        var judge = new StubJudge(new JudgeVerdict(false, 0.1, "misses the point"));

        var ex = await Assert.ThrowsAsync<EvalFailedException>(
            () => Expect.That("some output").Satisfies("some criteria", judge));

        Assert.Contains("misses the point", ex.Message);
    }

    [Fact]
    public async Task Throws_when_no_judge_configured()
    {
        var previous = NetEvalConfig.DefaultJudge;
        NetEvalConfig.DefaultJudge = null;
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Expect.That("output").Satisfies("criteria"));
        }
        finally
        {
            NetEvalConfig.DefaultJudge = previous;
        }
    }
}
