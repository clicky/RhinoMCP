using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class GetObjectsTool : IMcpTool
{
    public string Name => "get_objects";
    public string Description => "List objects in the active Rhino document with metadata.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            limit = new { type = "integer", description = "Max objects to return (default 500)" }
        }
    };

    public object Execute(JsonObject? args)
    {
        var limit = args?["limit"]?.GetValue<int>() ?? 500;
        var doc   = RhinoDoc.ActiveDoc;

        var settings = new ObjectEnumeratorSettings
        {
            ActiveObjects  = true,
            HiddenObjects  = true,
            LockedObjects  = true,
            DeletedObjects = false,
            IncludeLights  = true,
            IncludeGrips   = false,
            IncludePhantoms = false,
        };

        var objects = doc.Objects.GetObjectList(settings)
            .Take(limit)
            .Select(obj =>
            {
                var bb  = obj.Geometry?.GetBoundingBox(true) ?? BoundingBox.Unset;
                var idx = obj.Attributes.LayerIndex;
                var layerPath = idx >= 0 && idx < doc.Layers.Count
                    ? doc.Layers[idx].FullPath
                    : "Unknown";

                return new
                {
                    id      = obj.Id.ToString(),
                    name    = obj.Name ?? "",
                    layer   = layerPath,
                    type    = obj.Geometry?.GetType().Name ?? "Unknown",
                    visible = obj.Visible,
                    bbox    = bb.IsValid ? new
                    {
                        min = new { x = bb.Min.X, y = bb.Min.Y, z = bb.Min.Z },
                        max = new { x = bb.Max.X, y = bb.Max.Y, z = bb.Max.Z }
                    } : null
                };
            })
            .ToArray();

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(objects) } } };
    }
}
