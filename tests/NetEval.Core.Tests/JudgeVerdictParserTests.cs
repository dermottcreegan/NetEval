using NetEval;
using Xunit;

namespace NetEval.Core.Tests;

public class JudgeVerdictParserTests
{
    [Fact]
    public void Parses_plain_json()
    {
        var verdict = JudgeVerdictParser.Parse("""{"passed": true, "score": 0.9, "reasoning": "Covers the policy."}""");

        Assert.True(verdict.Passed);
        Assert.Equal(0.9, verdict.Score);
        Assert.Equal("Covers the policy.", verdict.Reasoning);
    }

    [Fact]
    public void Parses_json_wrapped_in_code_fences()
    {
        var reply = """
            ```json
            {"passed": false, "score": 0.2, "reasoning": "No mention of refunds."}
            ```
            """;

        var verdict = JudgeVerdictParser.Parse(reply);

        Assert.False(verdict.Passed);
        Assert.Equal(0.2, verdict.Score);
    }

    [Fact]
    public void Clamps_out_of_range_scores()
    {
        var verdict = JudgeVerdictParser.Parse("""{"passed": true, "score": 1.7, "reasoning": ""}""");

        Assert.Equal(1.0, verdict.Score);
    }

    [Fact]
    public void TryParse_returns_false_for_non_json()
    {
        Assert.False(JudgeVerdictParser.TryParse("I think it passes!", out _));
    }
}
