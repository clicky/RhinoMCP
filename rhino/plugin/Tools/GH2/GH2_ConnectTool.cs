using RhinoAI.Resources;

using Grasshopper2.Doc;
using Grasshopper2.Parameters;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class GH2_ConnectTool
{
    public record struct Endpoint(Guid Id, string Param);
    public record struct Wired(Endpoint Src, Endpoint Dst);

    [McpServerTool("g2_connect", "Connect GH2 Wire", false, false)]
    [Description("Wire an output parameter to an input parameter on the active GH2 canvas. 'src' and 'dst' may be a numeric index or a Name/UserName. For pure params (e.g. a slider) pass '' or '0'.")]
    public static IToolResult Connect(
        RhinoDoc rhDoc,
        [Description("Guid of the source IDocumentObject.")] string src_id,
        [Description("Output identifier: numeric index, output Name, or UserName. Use '' or '0' for pure params.")] string src,
        [Description("Guid of the destination IDocumentObject.")] string dst_id,
        [Description("Input identifier: numeric index, input Name, or UserName. Use '' or '0' for pure params.")] string dst,
        [Description("If true (the default), any sources already wired into the destination input are removed first, so this becomes its only wire. Pass false to add alongside them.")] bool replace = true,
        [Description("If true, trigger a new solution after wiring. Set false to batch multiple operations and solve once at the end.")] bool solve = true)
    {
        if (!GH2_Utils.TryGetDoc(rhDoc, out Document doc))
            return GH2_Failures.NoDocument;

        if (!Guid.TryParse(src_id, out Guid srcGuid))
            return Failure(ToolError.BadArgument, $"Invalid src_id guid '{src_id}'");

        if (!Guid.TryParse(dst_id, out Guid dstGuid))
            return Failure(ToolError.BadArgument, $"Invalid dst_id guid '{dst_id}'");

        IDocumentObject srcObj = doc.Objects.Find(srcGuid);
        if (srcObj is null)
            return Failure(ToolError.GH_Object_NotFound, $"Source object '{srcGuid}' not found");

        IDocumentObject dstObj = doc.Objects.Find(dstGuid);
        if (dstObj is null)
            return Failure(ToolError.GH_Object_NotFound, $"Destination object '{dstGuid}' not found");

        if (GH2_GraphOps.WouldCycle(doc, srcObj, dstObj))
            return Failure(ToolError.BadArgument, $"Wiring '{srcGuid}' into '{dstGuid}' would create a cycle, because the source already depends on the destination", "Grasshopper cannot solve a cyclic graph");

        if (!GH2_GraphOps.TryResolveOutput(srcObj, src, out IParameter? srcParam, out string srcErr))
            return Failure(ToolError.GH_Param_NotFound, srcErr);

        if (!GH2_GraphOps.TryResolveInput(dstObj, dst, out IParameter? dstParam, out string dstErr))
            return Failure(ToolError.GH_Param_NotFound, dstErr);

        int removed = 0;
        try
        {
            removed = GH2_GraphOps.Connect(srcParam!, dstParam!, replace);

            if (solve)
                doc.Solution.Start();
            GH2_Utils.Redraw();
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }

        return Success(
            new Wired(
                new Endpoint(srcObj.InstanceId, srcParam!.Nomen.Name),
                new Endpoint(dstObj.InstanceId, dstParam!.Nomen.Name)),
            removed > 0 ? $"Replaced {removed} source(s) already wired into '{dstParam!.Nomen.Name}'" : null);
    }
}
