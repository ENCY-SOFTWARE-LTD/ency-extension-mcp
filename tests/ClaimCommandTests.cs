using EncyExtensionMcp;
using Xunit;

public class ClaimCommandTests
{
    private static async Task<(int Code, string Out)> Run(string[] args, FakeStoreClient store, string? token)
    {
        var lines = new List<string>();
        int code = await ClaimCommand.Run(args, store, () => Task.FromResult(token), lines.Add);
        return (code, string.Join("\n", lines));
    }

    [Fact]
    public async Task ClaimsTheNameAndTellsTheAuthorNoSecretIsNeeded()
    {
        var store = new FakeStoreClient();
        var (code, output) = await Run(new[] { "claim", "MyExt", "acme/MyExt" }, store, "tok");

        Assert.Equal(0, code);
        Assert.Equal(("MyExt", "acme/MyExt"), store.Claims.Single());
        Assert.Contains("no repository secret", output);
    }

    [Fact]
    public async Task RejectsSomethingThatIsNotARepository()
    {
        var store = new FakeStoreClient();
        var (code, output) = await Run(new[] { "claim", "MyExt", "MyExt" }, store, "tok");

        Assert.Equal(2, code);
        Assert.Contains("owner/name", output);
        Assert.Empty(store.Claims);
    }

    [Fact]
    public async Task AsksForALoginWhenThereIsNone()
    {
        var (code, output) = await Run(new[] { "claim", "MyExt", "acme/MyExt" }, new FakeStoreClient(), null);

        Assert.Equal(1, code);
        Assert.Contains("ency-extension-mcp login", output);
    }

    [Fact]
    public async Task ReportsWhatTheStoreSaidWhenTheNameIsTaken()
    {
        var store = new FakeStoreClient { ClaimFailure = "403 Forbidden: claimed by someone else" };
        var (code, output) = await Run(new[] { "claim", "Taken", "acme/Taken" }, store, "tok");

        Assert.Equal(1, code);
        Assert.Contains("claimed by someone else", output);
    }

    [Fact]
    public async Task PrintsUsageWithoutArguments()
    {
        var (code, output) = await Run(new[] { "claim" }, new FakeStoreClient(), "tok");

        Assert.Equal(2, code);
        Assert.Contains(ClaimCommand.Usage, output);
    }
}
