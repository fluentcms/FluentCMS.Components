namespace FluentCMS.Components.Tests;

public sealed class CiGateProbe
{
    [Fact]
    public void DeliberateFailureForIssue37() => Assert.Fail("Deliberate failing test for issue 37");
}
