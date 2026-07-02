using Xunit.Abstractions;
using Xunit.Sdk;

namespace NetEval.Xunit;

/// <summary>
/// Buffers one attempt's messages so <see cref="LlmFactTestCase"/> can report only the
/// deciding attempt instead of every retry.
/// </summary>
internal sealed class CapturingMessageBus : IMessageBus
{
    private readonly List<IMessageSinkMessage> _messages = [];

    public bool QueueMessage(IMessageSinkMessage message)
    {
        lock (_messages)
            _messages.Add(message);
        return true;
    }

    /// <summary>Forwards the buffered messages, optionally annotating a failure with aggregate stats.</summary>
    public void ReplayTo(IMessageBus target, string? prependToFailure = null)
    {
        foreach (var message in _messages)
            target.QueueMessage(
                prependToFailure is not null && message is ITestFailed failed && failed.Messages.Length > 0
                    ? Annotate(failed, prependToFailure)
                    : message);
    }

    private static TestFailed Annotate(ITestFailed failed, string annotation)
    {
        var messages = (string[])failed.Messages.Clone();
        messages[0] = annotation + Environment.NewLine + messages[0];
        return new TestFailed(
            failed.Test, failed.ExecutionTime, failed.Output,
            failed.ExceptionTypes, messages, failed.StackTraces, failed.ExceptionParentIndices);
    }

    public void Dispose() { }
}
