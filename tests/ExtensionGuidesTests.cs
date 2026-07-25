using EncyExtensionMcp;
using Xunit;

public class ExtensionGuidesTests
{
    [Fact]
    public void IndexIsLoadedAndCarriesVerificationStamp()
    {
        Assert.NotEmpty(ExtensionGuides.All);
        Assert.Contains("cam-api-examples", ExtensionGuides.VerifiedAgainst);
    }

    [Theory]
    [InlineData("utility")]
    [InlineData("Utility")]
    [InlineData("UTILITY")]
    public void ResolvesUtilityByAnySpelling(string spelling)
    {
        Assert.Equal("utility", ExtensionGuides.Find(spelling)?.Key);
    }

    [Fact]
    public void UnknownTypeResolvesToNull()
    {
        Assert.Null(ExtensionGuides.Find("nosuchtype"));
        Assert.Null(ExtensionGuides.GetMarkdown("nosuchtype"));
    }

    [Fact]
    public void EveryRegisteredGuideHasMarkdownWithAllRequiredSections()
    {
        foreach (var g in ExtensionGuides.All)
        {
            string md = ExtensionGuides.GetMarkdown(g.Key)
                        ?? throw new Xunit.Sdk.XunitException($"no markdown for {g.Key}");
            foreach (var section in ExtensionGuides.RequiredSections)
                Assert.Contains(section, md);
        }
    }

    [Fact]
    public void ListMentionsEveryGuideAndItsDescription()
    {
        string list = ExtensionGuides.RenderList();
        foreach (var g in ExtensionGuides.All)
        {
            Assert.Contains(g.Key, list);
            Assert.Contains(g.Description, list);
        }
    }
}
