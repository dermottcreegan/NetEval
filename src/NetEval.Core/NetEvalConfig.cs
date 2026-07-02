namespace NetEval;

/// <summary>
/// Ambient configuration for eval runs. Set once in a test fixture or module initializer.
/// </summary>
public static class NetEvalConfig
{
    /// <summary>The judge used by <see cref="Expect"/> when none is passed explicitly.</summary>
    public static IJudge? DefaultJudge { get; set; }
}
