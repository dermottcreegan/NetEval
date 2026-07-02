namespace NetEval;

/// <summary>Thrown when an eval run regresses against its committed baseline, or a required baseline is missing.</summary>
public sealed class BaselineRegressionException(string message) : Exception(message);
