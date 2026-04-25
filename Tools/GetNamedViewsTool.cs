using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;

namespace RhMcp.Tools;

public sealed class GetNamedViewsTool : IMcpTool
{
    public string Name => "get_named_views";
    public string Description => "List named views in the document with camera position, target, and projection type.";
    public object InputSchema => new { type = "object", properties = new { } };

    public object Execute(JsonObject? args)
    {
        var doc   = RhinoDoc.ActiveDoc;
        var views = Enumerable.Range(0, doc.NamedViews.Count)
            .Select(i => doc.NamedViews[i])
            .Select(v =>
            {
                var vp  = v.Viewport;
                var cam = vp.CameraLocation;
                var tgt = vp.TargetPoint;
                return new
                {
                    name       = v.Name,
                    isParallel = vp.IsParallelProjection,
                    camera     = new { x = cam.X, y = cam.Y, z = cam.Z },
                    target     = new { x = tgt.X, y = tgt.Y, z = tgt.Z },
                    lensLength = vp.Camera35mmLensLength
                };
            })
            .ToArray();

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(views) } } };
    }
}
