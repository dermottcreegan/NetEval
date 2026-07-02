namespace NetEval;

/// <summary>Token usage from a judge model call, for cost tracking.</summary>
public sealed record JudgeUsage(long InputTokens, long OutputTokens)
{
    public static readonly JudgeUsage Zero = new(0, 0);

    public long TotalTokens => InputTokens + OutputTokens;

    public static JudgeUsage operator +(JudgeUsage a, JudgeUsage b) =>
        new(a.InputTokens + b.InputTokens, a.OutputTokens + b.OutputTokens);

    /// <summary>Estimated cost in USD, given the judge model's per-million-token prices.</summary>
    public double CostUsd(double inputPricePerMTok, double outputPricePerMTok) =>
        InputTokens / 1e6 * inputPricePerMTok + OutputTokens / 1e6 * outputPricePerMTok;
}
