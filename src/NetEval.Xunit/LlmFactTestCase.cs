using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace NetEval.Xunit;

/// <summary>
/// Test case that runs the method up to <c>Runs</c> times and passes when the pass rate
/// meets <c>PassThreshold</c>. Stops as soon as the outcome is decided in either direction,
/// so a clearly passing (or clearly failing) test doesn't burn extra LLM calls.
/// Only the deciding attempt's messages are reported; a failure is annotated with the
/// aggregate pass count.
/// </summary>
public sealed class LlmFactTestCase : XunitTestCase
{
    private int _runs;
    private double _passThreshold;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public LlmFactTestCase() { }

    public LlmFactTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        int runs,
        double passThreshold)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
        _runs = Math.Max(1, runs);
        _passThreshold = passThreshold;
    }

    /// <summary>How many of <paramref name="runs"/> attempts must pass. Always at least one.</summary>
    internal static int RequiredPasses(int runs, double passThreshold) =>
        Math.Max(1, (int)Math.Ceiling(Math.Clamp(passThreshold, 0.0, 1.0) * runs));

    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        if (_runs <= 1 || SkipReason is not null)
            return await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);

        var requiredPasses = RequiredPasses(_runs, _passThreshold);
        var allowedFailures = _runs - requiredPasses;

        var passes = 0;
        var failures = 0;
        var totalTime = 0m;

        for (var attempt = 1; attempt <= _runs; attempt++)
        {
            var capture = new CapturingMessageBus();
            var summary = await base.RunAsync(diagnosticMessageSink, capture, constructorArguments, aggregator, cancellationTokenSource);
            totalTime += summary.Time;

            // Harness-level error (not a test failure) — report this attempt as-is.
            if (aggregator.HasExceptions || cancellationTokenSource.IsCancellationRequested)
            {
                capture.ReplayTo(messageBus);
                return summary;
            }

            if (summary.Failed == 0)
                passes++;
            else
                failures++;

            if (passes >= requiredPasses)
            {
                capture.ReplayTo(messageBus); // last attempt necessarily passed
                return new RunSummary { Total = 1, Time = totalTime };
            }

            if (failures > allowedFailures)
            {
                capture.ReplayTo(messageBus, prependToFailure:
                    $"[LlmFact] {passes}/{attempt} runs passed; needed {requiredPasses}/{_runs} (PassThreshold = {_passThreshold:0.##}).");
                return new RunSummary { Total = 1, Failed = 1, Time = totalTime };
            }

            diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                "[LlmFact] '{0}' attempt {1}/{2}: {3} so far, need {4} passes",
                DisplayName, attempt, _runs, passes, requiredPasses));
        }

        // Unreachable: by the last attempt either passes >= required or failures > allowed.
        throw new InvalidOperationException($"[LlmFact] '{DisplayName}' completed {_runs} runs without a decision.");
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        base.Serialize(data);
        data.AddValue("Runs", _runs);
        data.AddValue("PassThreshold", _passThreshold);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        base.Deserialize(data);
        _runs = data.GetValue<int>("Runs");
        _passThreshold = data.GetValue<double>("PassThreshold");
    }
}
