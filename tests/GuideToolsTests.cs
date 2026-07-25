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
    public void UnknownTypeThrowsWithTheAllowedValues()
    {
        var ex = Assert.Throws<ArgumentException>(() => _tools.GetExtensionGuide("toolpath-thing"));
        Assert.Contains("utility", ex.Message);
        Assert.Contains("list", ex.Message);
    }

    [Fact]
    public void EmptyTypeIsTreatedAsList()
    {
        Assert.Contains("| type |", _tools.GetExtensionGuide(""));
    }
}
