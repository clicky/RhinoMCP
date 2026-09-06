using Rhino.DocObjects;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class ListObjectsTool
{
    [McpServerTool("list_objects", "List Document Objects", true, false)]
    [Description("List objects in the active document. Filter by name, layer, or geometry type. Pure query — does not change selection or viewport.")]
    public static IToolResult ListObjects(
        RhinoDoc doc,
        [Description("Object names to match")] string[]? names = null,
        [Description("Layer full path")] string? layer = null,
        [Description("Filter by type: point, pointset, curve, surface, brep, mesh, annotation, light, block")] string? geometryType = null,
        [Description("Include hidden objects (default false)")] bool includeHidden = false,
        [Description("Include locked objects (default true)")] bool includeLocked = true,
        [Description("Maximum number of objects to return (default 1000)")] int limit = 1000)
    {
        if (limit < 1)
            return Failure(ToolError.BadArgument, $"limit must be 1 or more, got {limit}");

        ObjectEnumeratorSettings settings = new()
        {
            ActiveObjects = true,
            HiddenObjects = includeHidden,
            LockedObjects = includeLocked,
            DeletedObjects = false,
            IncludeLights = true,
            IncludeGrips = false,
        };

        string? warning = null;
        if (!string.IsNullOrEmpty(geometryType))
        {
            if (TryParseObjectType(geometryType, out ObjectType filter))
                settings.ObjectTypeFilter = filter;
            else
                warning = $"Unknown geometryType: {geometryType}. Returning all object types unfiltered.";
        }

        if (!string.IsNullOrEmpty(layer))
        {
            int idx = doc.Layers.FindByFullPath(layer, RhinoMath.UnsetIntIndex);
            if (idx >= 0)
                settings.LayerIndexFilter = idx;
            else
                warning = warning is null ? $"Layer not found: {layer}" : $"{warning} Layer not found: {layer}";
        }

        HashSet<string> nameSet = (names ?? []).ToHashSet(StringComparer.Ordinal);

        IEnumerable<RhinoObject> matches = doc.Objects.GetObjectList(settings)
            .Where(o => nameSet.Count == 0 || nameSet.Contains(o.Name ?? string.Empty));

        bool truncated = false;
        var results = matches
            .Take(limit + 1)
            .Select(o => new
            {
                id = o.Id.ToString(),
                name = o.Name ?? string.Empty,
                layer = doc.Layers[o.Attributes.LayerIndex].FullPath,
                type = o.Geometry?.GetType().Name ?? "Unknown",
            })
            .ToArray();

        if (results.Length > limit)
        {
            truncated = true;
            results = results.Take(limit).ToArray();
        }

        return Success(new
        {
            count = results.Length,
            truncated,
            objects = results,
        }, warning);
    }

    private static bool TryParseObjectType(string s, out ObjectType type)
    {
        switch (s.ToLowerInvariant())
        {
            case "point": type = ObjectType.Point; return true;
            case "pointset": type = ObjectType.PointSet; return true;
            case "curve": type = ObjectType.Curve; return true;
            case "surface": type = ObjectType.Surface; return true;
            case "brep": type = ObjectType.Brep; return true;
            case "mesh": type = ObjectType.Mesh; return true;
            case "annotation": type = ObjectType.Annotation; return true;
            case "light": type = ObjectType.Light; return true;
            case "block": type = ObjectType.InstanceReference; return true;
            default: type = ObjectType.None; return false;
        }
    }
}
