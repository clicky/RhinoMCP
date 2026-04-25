using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Display;

namespace RhMcp.Tools;

public sealed class GetLayoutsTool : IMcpTool
{
    public string Name => "get_layouts";
    public string Description => "List page layouts in the document with their detail views.";
    public object InputSchema => new { type = "object", properties = new { } };

    public object Execute(JsonObject? args)
    {
        var doc     = RhinoDoc.ActiveDoc;
        var layouts = doc.Views.GetPageViews().Select(p => new
        {
            id     = p.MainViewport.Id.ToString(),
            name   = p.PageName,
            width  = p.PageWidth,
            height = p.PageHeight,
            details = p.GetDetailViews().Select(d => new
            {
                id         = d.Id.ToString(),
                isLocked   = d.DetailGeometry?.IsProjectionLocked ?? false,
                isParallel = d.Viewport?.IsParallelProjection ?? false
            }).ToArray()
        }).ToArray();

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(layouts) } } };
    }
}
