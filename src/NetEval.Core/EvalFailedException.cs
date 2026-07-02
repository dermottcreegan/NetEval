namespace NetEval;

/// <summary>Thrown when an output fails a semantic assertion. Message includes the judge's reasoning.</summary>
public sealed class EvalFailedException(string criteria, string output, JudgeVerdict verdict)
    : Exception(BuildMessage(criteria, output, verdict))
{
    public string Criteria { get; } = criteria;
    public string Output { get; } = output;
    public JudgeVerdict Verdict { get; } = verdict;

    private static string BuildMessage(string criteria, string output, JudgeVerdict verdict) =>
        $"""
        Output did not satisfy criteria.
          Criteria:  {criteria}
          Score:     {verdict.Score:0.00}
          Reasoning: {verdict.Reasoning}
          Output:    {Truncate(output)}
        """;

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "…";
}
