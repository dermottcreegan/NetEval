using Xunit.Abstractions;
using Xunit.Sdk;

namespace NetEval.Xunit;

/// <summary>Creates <see cref="LlmFactTestCase"/>s carrying the attribute's Runs/PassThreshold.</summary>
public sealed class LlmFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    public IEnumerable<IXunitTestCase> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
    {
        yield return new LlmFactTestCase(
            diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            factAttribute.GetNamedArgument<int>("Runs"),
            factAttribute.GetNamedArgument<double>("PassThreshold"));
    }
}
