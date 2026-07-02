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
    [LlmFact]
    public async Task Refund_reply_mentions_policy_and_avoids_legal_advice()
    {
        var reply = FakeSupportBot.Reply("I want a refund for my order, it arrived broken.");

        await Expect.That(reply).Satisfies(
            "acknowledges the customer's problem, mentions the 30-day refund policy, and gives no legal advice",
            judge: new ClaudeJudge());
    }
}

/// <summary>Stand-in for the user's real AI feature (in real projects: an IChatClient-backed bot).</summary>
internal static class FakeSupportBot
{
    public static string Reply(string customerMessage) =>
        "I'm sorry your order arrived broken! You're covered by our 30-day refund policy — " +
        "I've started the process and you'll get a confirmation email shortly.";
}
