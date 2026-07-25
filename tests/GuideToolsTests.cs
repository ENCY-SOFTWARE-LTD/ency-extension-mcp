using EncyExtensionMcp;
using Xunit;

public class GuideToolsTests
{
    private readonly GuideTools _tools = new();

    [Fact]
    public void ReturnsTheGuideMarkdown()
    {
        string md = _tools.GetExtensionGuide("utility");
        Assert.Contains("## Skeleton", md);
    }

    [Fact]
    public void ListReturnsEveryRegisteredType()
    {
        string list = _tools.GetExtensionGuide("list");
        foreach (var g in ExtensionGuides.All) Assert.Contains(g.Key, list);
    }

    [Fact]
    public void UnknownTypeAnswersWithTheAllowedValuesInsteadOfThrowing()
    {
        // Thrown text is replaced by the MCP host with a generic "an error occurred", so the hint has
        // to travel as a normal result for the agent to be able to correct itself.
        string answer = _tools.GetExtensionGuide("toolpath-thing");
        Assert.Contains("Unknown extension type 'toolpath-thing'", answer);
        foreach (var g in ExtensionGuides.All) Assert.Contains(g.Key, answer);
        Assert.Contains("list", answer);
    }

    [Fact]
    public void EmptyTypeIsTreatedAsList()
    {
        Assert.Contains("| type |", _tools.GetExtensionGuide(""));
    }
}
