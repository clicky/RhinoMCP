using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class GetObjectDetailsTool : IMcpTool
{
    public string Name => "get_object_details";
    public string Description => "Return detailed geometry info for a specific object by GUID: area, volume, vertex/face counts, bounding box.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            id = new { type = "string", description = "Object GUID" }
        },
        required = new[] { "id" }
    };

    public object Execute(JsonObject? args)
    {
        var idStr = args?["id"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing required arg: id");

        if (!Guid.TryParse(idStr, out var guid))
            throw new ArgumentException($"Invalid GUID: {idStr}");

        var doc = RhinoDoc.ActiveDoc;
        var obj = doc.Objects.FindId(guid)
            ?? throw new InvalidOperationException($"Object not found: {idStr}");

        var geo = obj.Geometry;
        var bb  = geo?.GetBoundingBox(true) ?? BoundingBox.Unset;

        double? area        = null;
        double? volume      = null;
        int?    vertexCount = null;
        int?    faceCount   = null;

        switch (geo)
        {
            case Brep brep:
                area = AreaMassProperties.Compute(brep)?.Area;
                if (brep.IsSolid)
                    volume = VolumeMassProperties.Compute(brep)?.Volume;
                break;

            case Mesh mesh:
                area        = AreaMassProperties.Compute(mesh)?.Area;
                vertexCount = mesh.Vertices.Count;
                faceCount   = mesh.Faces.Count;
                if (mesh.IsClosed)
                    volume = VolumeMassProperties.Compute(mesh)?.Volume;
                break;

            case Surface srf:
                area = AreaMassProperties.Compute(srf)?.Area;
                break;

            case Curve crv:
                area = crv.GetLength();
                break;
        }

        var details = new
        {
            id          = obj.Id.ToString(),
            name        = obj.Name ?? "",
            layer       = doc.Layers[obj.Attributes.LayerIndex].FullPath,
            type        = geo?.GetType().Name ?? "Unknown",
            area,
            volume,
            vertexCount,
            faceCount,
            bbox = bb.IsValid ? new
            {
                min  = new { x = bb.Min.X, y = bb.Min.Y, z = bb.Min.Z },
                max  = new { x = bb.Max.X, y = bb.Max.Y, z = bb.Max.Z },
                size = new { x = bb.Max.X - bb.Min.X, y = bb.Max.Y - bb.Min.Y, z = bb.Max.Z - bb.Min.Z }
            } : null
        };

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(details) } } };
    }
}
