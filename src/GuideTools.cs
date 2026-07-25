using System.ComponentModel;
using ModelContextProtocol.Server;

namespace EncyExtensionMcp;

/// <summary>
/// Serves the extension-type skill library to the agent. Stateless: every guide is embedded in this
/// assembly, so the tool works offline and updates together with the tool version.
/// </summary>
[McpServerToolType]
public class GuideTools
{
    [McpServerTool(Name = "get_extension_guide"), Description(
        "How to write a specific kind of ENCY extension: which entry point to implement, how to "
        + "register it in the settings json and the factory, a minimal skeleton and the traps. "
        + "Call it with type=list first to pick the right entry point, then again with that type. "
        + "Use this BEFORE writing extension code.")]
    public string GetExtensionGuide(
        [Description("Entry point: utility, global, utility_runner, operation_popup, "
                     + "geom_model_node_popup, operation_solver, cldata_converter, plm; "
                     + "or 'cookbook' for the cross-cutting rules; or 'list' to see all of them")]
        string type = "list")
    {
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Equals("list", StringComparison.OrdinalIgnoreCase))
            return ExtensionGuides.RenderList();

        // A wrong type is answered, not thrown: the MCP host replaces exception text with a generic
        // "an error occurred", so a thrown hint never reaches the agent. Returning it does.
        return ExtensionGuides.GetMarkdown(type) ?? UnknownType(type);
    }

    private static string UnknownType(string type) =>
        $"Unknown extension type '{type}'. Call get_extension_guide again with one of:\n\n"
        + string.Join("\n", ExtensionGuides.All.Select(g => $"- `{g.Key}` — {g.Description}"))
        + "\n- `list` — the same table in one place";
}
