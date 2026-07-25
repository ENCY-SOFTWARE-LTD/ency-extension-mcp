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

        return ExtensionGuides.GetMarkdown(type)
               ?? throw new ArgumentException(
                   $"Unknown extension type '{type}'. Allowed: "
                   + string.Join(", ", ExtensionGuides.All.Select(g => g.Key))
                   + ", list.");
    }
}
