namespace EncyExtensionMcp;

/**
 * `ency-extension-mcp claim <PackageId> <owner/repo>` — bind a GitHub repository to an extension name
 * from the author's own machine, so the repository never has to hold a store token: the workflow
 * authenticates with its GitHub OIDC token from the very first publish.
 *
 * For authors who created the repo from the template by hand (Cursor users get this via
 * create_extension_repo). Not an MCP tool: it runs before any editor is involved.
 */
public static class ClaimCommand
{
    public const string Usage = "usage: ency-extension-mcp claim <PackageId> <owner/repo>";

    public static async Task<int> Run(string[] args, IStoreClient store, Func<Task<string?>> token,
                                     Action<string> write)
    {
        if (args.Length < 3)
        {
            write(Usage);
            return 2;
        }
        string packageId = args[1].Trim(), repository = args[2].Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(repository, @"^[\w.\-]+/[\w.\-]+$"))
        {
            write($"'{repository}' is not a repository — expected owner/name.\n{Usage}");
            return 2;
        }

        string? access;
        try { access = await token(); }
        catch (InvalidOperationException e) { write(e.Message); return 1; }
        if (string.IsNullOrWhiteSpace(access))
        {
            write("No store login on this machine. Run `ency-extension-mcp login` first.");
            return 1;
        }

        string? failure = await store.ClaimPackage(packageId, repository, access);
        if (failure != null)
        {
            write($"Could not claim {packageId} for {repository}: {failure}");
            return 1;
        }
        write($"{repository} is now the trusted publisher of {packageId}. "
              + "Push a version tag — CI publishes with no repository secret.");
        return 0;
    }
}
