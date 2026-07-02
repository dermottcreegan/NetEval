using Microsoft.Extensions.AI;

namespace NetEval;

/// <summary>
/// Provider-agnostic judge that works with any <see cref="IChatClient"/>
/// (Anthropic, OpenAI, Azure, Ollama — anything implementing Microsoft.Extensions.AI).
/// </summary>
public sealed class ChatClientJudge(IChatClient chatClient) : IJudge
{
    public async Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        var prompt =
            $$"""
            You are a strict evaluator. Judge whether the OUTPUT satisfies the CRITERIA.
            {{(request.Input is null ? "" : $"INPUT (what produced the output):\n{request.Input}\n")}}
            {{(request.Expected is null ? "" : $"REFERENCE ANSWER (for comparison):\n{request.Expected}\n")}}
            CRITERIA:
            {{request.Criteria}}

            OUTPUT:
            {{request.Output}}

            Respond with ONLY a JSON object, no code fences, in this exact shape:
            {"passed": true|false, "score": 0.0-1.0, "reasoning": "one or two sentences"}
            """;

        var response = await chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

        var verdict = JudgeVerdictParser.Parse(response.Text);
        return response.Usage is { } usage
            ? verdict with { Usage = new JudgeUsage(usage.InputTokenCount ?? 0, usage.OutputTokenCount ?? 0) }
            : verdict;
    }
}
