using NetEval;
using NetEval.Judges.Claude;
using NetEval.Xunit;
using Xunit;

namespace SupportBot.Evals;

/// <summary>
/// The demo everyone copies: eval a support-bot reply against natural-language criteria.
/// [LlmFact] auto-skips when ANTHROPIC_API_KEY is not set, so this compiles and runs green
/// everywhere; with a key set it calls Claude as the judge.
/// </summary>
public class SupportBotEvals
{
    // Runs 3 times; passes if at least 2 runs pass — absorbs judge non-determinism.
    [LlmFact(Runs = 3, PassThreshold = 0.66)]
    public async Task Refund_reply_mentions_policy_and_avoids_legal_advice()
    {
        var reply = FakeSupportBot.Reply("I want a refund for my order, it arrived broken.");

        await Expect.That(reply).Satisfies(
            "acknowledges the customer's problem, mentions the 30-day refund policy, and gives no legal advice",
            judge: new ClaudeJudge());
    }

    // Dataset-driven: run every case in the JSONL file, get pass rate, judge cost, and latency,
    // then gate against the committed baseline — CI fails if quality regresses.
    [LlmFact]
    public async Task Support_bot_does_not_regress_against_baseline()
    {
        var cases = JsonlDataset.Load("support_cases.jsonl");

        var report = await new EvalRunner(new ClaudeJudge())
            .RunAsync(cases, message => Task.FromResult(FakeSupportBot.Reply(message)));

        // First run creates support_bot.baseline.json next to this file — commit it.
        // After an intentional change, re-run with NETEVAL_UPDATE_BASELINES=1 to accept.
        report.VerifyBaseline("support_bot.baseline.json");
    }
}

/// <summary>Stand-in for the user's real AI feature (in real projects: an IChatClient-backed bot).</summary>
internal static class FakeSupportBot
{
    public static string Reply(string customerMessage) =>
        customerMessage.Contains("refund", StringComparison.OrdinalIgnoreCase)
            ? "I'm sorry for the trouble! You're covered by our 30-day refund policy — " +
              "I've started the process and you'll get a confirmation email shortly."
            : "I'm so sorry about that! I've escalated this to our shipping team and " +
              "you'll hear back with tracking details within one business day.";
}
