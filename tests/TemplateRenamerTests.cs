using EncyExtensionMcp;
using Xunit;

public class TemplateRenamerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mcp-rn-" + Guid.NewGuid().ToString("N"));

    public TemplateRenamerTests()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "src"));
        File.WriteAllText(Path.Combine(_dir, "src", "EncyExtension.csproj"), "<Project>EncyExtension</Project>");
        File.WriteAllText(Path.Combine(_dir, "src", "EncyExtension.settings.json"), "{\"id\":\"Extension.Utility.EncyExtension\"}");
        File.WriteAllText(Path.Combine(_dir, "src", "Extension.cs"), "namespace EncyExtension; // EncyExtension");
        File.WriteAllText(Path.Combine(_dir, "src", "readme.md"), "no placeholder here");
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void RenamesContentsAndFileNames()
    {
        int touched = TemplateRenamer.Rename(_dir, "MyCoolExt");
        Assert.Equal(3, touched); // readme has no placeholder
        Assert.True(File.Exists(Path.Combine(_dir, "src", "MyCoolExt.csproj")));
        Assert.True(File.Exists(Path.Combine(_dir, "src", "MyCoolExt.settings.json")));
        Assert.Contains("Extension.Utility.MyCoolExt",
            File.ReadAllText(Path.Combine(_dir, "src", "MyCoolExt.settings.json")));
        Assert.DoesNotContain("EncyExtension", File.ReadAllText(Path.Combine(_dir, "src", "Extension.cs")));
    }

    [Theory]
    [InlineData("MyExt", true)]
    [InlineData("My.Ext2", true)]
    [InlineData("2Fast", false)]
    [InlineData("has space", false)]
    [InlineData("has-dash", false)]
    [InlineData("", false)]
    public void ValidatesNames(string name, bool ok) => Assert.Equal(ok, TemplateRenamer.IsValidName(name));

    [Fact]
    public void ThrowsWithoutSrcFolder()
    {
        var empty = Path.Combine(Path.GetTempPath(), "mcp-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);
        try { Assert.Throws<DirectoryNotFoundException>(() => TemplateRenamer.Rename(empty, "X")); }
        finally { Directory.Delete(empty, true); }
    }
}
