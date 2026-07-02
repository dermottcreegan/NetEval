using Anthropic;
using Microsoft.Extensions.AI;

namespace NetEval.Judges.Claude;

/// <summary>
/// LLM judge backed by the official Anthropic C# SDK (the "Anthropic" NuGet package).
/// Reads ANTHROPIC_API_KEY from the environment unless a client is supplied.
/// Adapts the Anthropic client to <see cref="IChatClient"/> and delegates to
/// <see cref="ChatClientJudge"/>, so the judge prompt lives in one place.
/// </summary>
public sealed class ClaudeJudge : IJudge, IDisposable
{
    public const string DefaultModel = "claude-opus-4-8";

    /// <summary>A verdict is a short JSON object; this leaves generous headroom.</summary>
    private const int MaxOutputTokens = 1024;

    private readonly IChatClient _chatClient;
    private readonly ChatClientJudge _inner;

    /// <param name="model">Judge model ID. Defaults to <see cref="DefaultModel"/>.</param>
    public ClaudeJudge(string model = DefaultModel)
        : this(new AnthropicClient(), model)
    {
    }

    /// <param name="client">A configured Anthropic client (e.g. with an explicit API key).</param>
    /// <param name="model">Judge model ID. Defaults to <see cref="DefaultModel"/>.</param>
    public ClaudeJudge(IAnthropicClient client, string model = DefaultModel)
    {
        _chatClient = client.AsIChatClient(model, MaxOutputTokens);
        _inner = new ChatClientJudge(_chatClient);
    }

    public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
        => _inner.JudgeAsync(request, cancellationToken);

    public void Dispose() => _chatClient.Dispose();
}
