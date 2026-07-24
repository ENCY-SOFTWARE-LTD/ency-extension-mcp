# ency-extension-mcp

MCP server for the "write an ENCY extension in Cursor, never copy a file by hand" flow
(see [ency-extension-template](https://github.com/ENCY-SOFTWARE-LTD/ency-extension-template)):

| Tool | What it does |
|---|---|
| `create_extension_repo` | GitHub repo from the ENCY template → waits for the copy → clones → renames the extension → sets the publish secret → pushes. |
| `publish_extension` | Tags `vX.Y.Z` and pushes — GitHub Actions builds, packs and publishes to the [ENCY Extension Store](https://dmc.encycam.com/store). |
| `publish_status` | Follows the run (failure log tail when red) and reports the store card + moderation state when green. |

Auth model: the server shells out to the **author's own `gh` and `git`** — your GitHub login is
the credential there. For the store, `ency-extension-mcp login` (once) performs a Keycloak
login and keeps only a refresh token; the server then mints fresh access tokens itself and
plants one as the secret of each new repo — needed for the FIRST publish only, after which the
repo publishes via GitHub OIDC with no secret at all. The author never touches a token.

## Setup (Cursor)

Prerequisites: .NET 8 SDK, `git`, `gh` (`gh auth login` once).

Install the tool (once it is published to the ENCY feed):

```bash
dotnet tool install -g EncySoftware.ExtensionStoreMcp --add-source https://nexus.encycam.com/repository/master/index.json
```

Until then, from a clone: `dotnet pack src -c Release -o pkg && dotnet tool install -g EncySoftware.ExtensionStoreMcp --add-source ./pkg`

Log in to the store once (your licsys account; only a refresh token is stored, under %APPDATA%):

```bash
ency-extension-mcp login
```

`.cursor/mcp.json` (project or global `~/.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "ency-extension-store": {
      "command": "ency-extension-mcp"
    }
  }
}
```

(The `ENCY_STORE_TOKEN` env var still overrides the stored login when set — CI/debug escape hatch.)

## The flow it enables

1. In Cursor: *"create an ENCY extension called ToolpathTimer"* → `create_extension_repo`
   makes the repo, clones it next to your workspace, renames everything, wires the secret.
2. Write the code in `src/` — the template carries Cursor rules that teach the agent the
   extension anatomy (factory, settings.json ids, package.info.json).
3. *"publish it as 0.1.0"* → `publish_extension` tags and pushes; CI does the rest.
4. *"did it publish?"* → `publish_status` → run status → store card link. New extensions land
   hidden until a store moderator approves them; the direct card link works immediately.

## Development

```bash
dotnet test tests/EncyExtensionMcp.Tests.csproj   # logic tests (processes faked)
dotnet run --project src                           # stdio server (speak JSON-RPC to it)
```

Config knobs: `ENCY_STORE_API` overrides the store API base (test stands).
