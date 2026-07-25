# ency-extension-mcp

MCP server for the "write an ENCY extension in Cursor, never copy a file by hand" flow
(see [ency-extension-template](https://github.com/ENCY-SOFTWARE-LTD/ency-extension-template)):

| Tool | What it does |
|---|---|
| `create_extension_repo` | GitHub repo from the ENCY template → waits for the copy → clones → renames the extension → sets the publish secret → pushes. |
| `publish_extension` | Tags `vX.Y.Z` and pushes — GitHub Actions builds, packs and publishes to the [ENCY Extension Store](https://dmc.encycam.com/store). |
| `publish_status` | Follows the run (failure log tail when red) and reports the store card + moderation state when green. |
| `get_extension_guide` | The skill library: which of the eight ENCY entry points to implement, how to register it, a minimal skeleton and the traps. `type=list` first, then the type. |

Auth model: the server shells out to the **author's own `gh` and `git`** — your GitHub login is
the credential there. For the store, `ency-extension-mcp login` (once) performs a Keycloak
login and keeps only a refresh token. `create_extension_repo` then **claims the extension name for
the new repository**, so no credential is stored in GitHub at all: every publish, the first one
included, authenticates with the workflow's own GitHub OIDC token. If the claim cannot be made (store
unreachable, name owned by somebody else) the tool falls back to planting an `ENCY_STORE_TOKEN`
secret, which covers the first publish. Either way the author never handles a token.

Repos made by hand from the template can be bound the same way:

```bash
ency-extension-mcp claim MyCoolExtension owner/MyCoolExtension
```

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
2. *"it should add an item to the right-click menu of an operation"* → `get_extension_guide`
   (`list` → `operation_popup`) tells the agent which interface to implement, which
   `*.settings.json` key to use and what breaks. The template carries the same guides as Cursor
   rules (`.cursor/rules/type-*.mdc`), generated from `guides/` here — the tool is the fresh copy.
3. Write the code in `src/` — the always-on rule covers the anatomy (factory, settings.json ids,
   package.info.json).
4. *"publish it as 0.1.0"* → `publish_extension` tags and pushes; CI does the rest.
5. *"did it publish?"* → `publish_status` → run status → store card link. New extensions land
   hidden until a store moderator approves them; the direct card link works immediately.

## Development

```bash
dotnet test tests/EncyExtensionMcp.Tests.csproj   # logic tests (processes faked)
dotnet run --project src                           # stdio server (speak JSON-RPC to it)
```

The extension-type guides in `guides/` are the single source of truth: they are embedded into the
assembly for `get_extension_guide`, and the template repo carries a generated snapshot of the same
text as Cursor rules. After editing a guide:

```bash
powershell -NoProfile -File tools/sync-rules.ps1          # .cursor/rules/*.mdc + AGENTS.md in the template
powershell -NoProfile -File tools/sync-rules.ps1 -Check    # exit 1 if the snapshot drifted
```

Two formats, one source: Cursor picks up `.cursor/rules/*.mdc` by itself, while Claude Code, Codex and
Copilot read `AGENTS.md` — so the generator also writes an `AGENTS.md` router (what the repo is, which
guide to open for which kind of extension, how publishing works). It links the same `.mdc` files
instead of copying them, so a guide edit never needs a second pass.

`-TemplateDir` points elsewhere if your template checkout is not a sibling of this repo. Commit the
template repo separately — the script only writes files. Adding a new entry point means: a guide file,
an entry in `guides/_index.json` (the tests read it), and a re-run of the script.

Config knobs: `ENCY_STORE_API` overrides the store API base (test stands).
