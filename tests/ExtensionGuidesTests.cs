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
            if (g.StoreType.Length == 0) continue;   // the cookbook is not a type guide
            foreach (var section in ExtensionGuides.RequiredSections)
                Assert.Contains(section, md);
        }
    }

    [Fact]
    public void CookbookCoversTheCrossCuttingTopics()
    {
        string md = ExtensionGuides.GetMarkdown("cookbook")!;
        foreach (var topic in new[] { "ComWrapper", "TResultStatus", "IExtensionLogger",
                                      "IExtensionLazyUnloadable", "STA" })
            Assert.Contains(topic, md);
    }

    [Fact]
    public void EveryEntryPointOfTheApiIsCovered()
    {
        // Eight entry points exist: seven in the upstream reference plus PLM (examples only).
        var expected = new[] { "utility", "global", "utility_runner", "operation_popup",
                               "geom_model_node_popup", "operation_solver", "cldata_converter", "plm" };
        var types = ExtensionGuides.All.Where(g => g.StoreType.Length > 0).Select(g => g.Key).ToList();
        Assert.Equal(expected.OrderBy(x => x), types.OrderBy(x => x));
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
