namespace NetEval;

/// <summary>One row of an eval dataset.</summary>
/// <param name="Input">Input handed to the system under test.</param>
/// <param name="Criteria">Criteria this case's output must satisfy (falls back to dataset-level criteria).</param>
/// <param name="Expected">Optional reference answer, available to the judge for comparison.</param>
public sealed record EvalCase(string Input, string? Criteria = null, string? Expected = null);
