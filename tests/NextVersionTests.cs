using EncyExtensionMcp;
using Xunit;

public class NextVersionTests
{
    [Theory]
    // the ordinary case: bump the last number
    [InlineData("v0.1.8\nv0.1.7\nv0.1.6\n", "0.1.9")]
    [InlineData("v1.0.0\n", "1.0.1")]
    [InlineData("v2.10.99\n", "2.10.100")]
    // no tags yet -> the first release
    [InlineData("", "0.1.0")]
    [InlineData("\n \n", "0.1.0")]
    // pre-release tags are not a base to bump from: 0.2.0-rc.1 -> 0.2.0
    [InlineData("v0.2.0-rc.1\n", "0.2.0")]
    // junk that is not a version is ignored
    [InlineData("nightly\nv0.3.4\n", "0.3.5")]
    public void PicksTheNextPatchFromTheTagList(string gitTagOutput, string expected)
        => Assert.Equal(expected, NextVersion.FromTags(gitTagOutput));

    /** git sorts by -v:refname, but a repo may hand back anything — do not trust the order. */
    [Fact]
    public void TakesTheHighestVersionNotTheFirstLine()
        => Assert.Equal("0.2.1", NextVersion.FromTags("v0.1.9\nv0.2.0\nv0.1.4\n"));
}
