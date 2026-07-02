# NetEval

**Test your AI features like you test your code.**

NetEval is an LLM eval framework for .NET that plugs into xUnit and your CI pipeline.
If your app has an AI feature and your "testing" is eyeballing outputs, this is for you.

```csharp
[LlmFact] // auto-skips when no API key; retry semantics for flaky LLM outputs
public async Task Refund_reply_mentions_policy_and_avoids_legal_advice()
{
    var reply = supportBot.Reply("I want a refund, my order arrived broken.");

    await Expect.That(reply).Satisfies(
        "acknowledges the problem, mentions the 30-day refund policy, and gives no legal advice");
}
```

Run it with `dotnet test`. Fail messages include the judge's reasoning:

```
NetEval.EvalFailedException : Output did not satisfy criteria.
  Criteria:  mentions the 30-day refund policy...
  Score:     0.20
  Reasoning: The reply apologises but never mentions any refund policy.
```

## Why

Python has a dozen eval frameworks. .NET teams shipping LLM features have essentially
nothing that fits how they already work: xUnit, `dotnet test`, CI gates. NetEval is that
missing piece — no dashboard, no hosted service, just tests.

- **Provider-agnostic**: the system under test and the judge both work through
  `Microsoft.Extensions.AI.IChatClient` — Anthropic, OpenAI, Azure, Ollama.
- **CI-native**: LLM tests auto-skip without API keys; baselines gate regressions (roadmap).
- **Honest about non-determinism**: run tests N times with a pass threshold instead of
  pretending LLM outputs are deterministic (roadmap).

## Packages

| Package | Purpose |
|---|---|
| `NetEval.Core` | Judges, semantic assertions, datasets |
| `NetEval.Xunit` | `[LlmFact]` attribute and xUnit integration |
| `NetEval.Judges.Claude` | Default judge backed by the official Anthropic C# SDK |

## Status / Roadmap

Early development — pre-alpha, API will change.

- [x] Semantic assertions (`Expect.That(...).Satisfies(...)`)
- [x] Provider-agnostic `ChatClientJudge` over `IChatClient`
- [x] `[LlmFact]` with API-key auto-skip
- [x] JSONL datasets
- [ ] `ClaudeJudge` wired to the Anthropic SDK (week 1)
- [ ] Retry semantics: `[LlmFact(Runs = 5, PassThreshold = 0.8)]` (week 2)
- [ ] Dataset runner with pass rate, cost, and latency reporting (week 2)
- [ ] Baseline snapshots + CI regression gating (week 3)
- [ ] NuGet release (week 4)

## License

MIT
