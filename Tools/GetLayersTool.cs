using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;

namespace RhMcp.Tools;

public sealed class GetLayersTool : IMcpTool
{
    public string Name => "get_layers";
    public string Description => "Return the full layer tree with colors, visibility, and lock state.";
    public object InputSchema => new { type = "object", properties = new { } };

    public object Execute(JsonObject? args)
    {
        var doc    = RhinoDoc.ActiveDoc;
        var layers = doc.Layers
            .Where(l => !l.IsDeleted)
            .Select(l => new
            {
                id        = l.Id.ToString(),
                name      = l.Name,
                fullPath  = l.FullPath,
                color     = new { r = l.Color.R, g = l.Color.G, b = l.Color.B },
                visible   = l.IsVisible,
                locked    = l.IsLocked,
                isCurrent = l.Index == doc.Layers.CurrentLayerIndex,
                parentId  = l.ParentLayerId.ToString()
            })
            .ToArray();

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(layers) } } };
    }
}
