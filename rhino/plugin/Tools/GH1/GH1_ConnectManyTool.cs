using RhinoAI.Resources;

using Grasshopper.Kernel;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH1_ConnectManyTool
{
    public record struct WireSpec(string SrcId, string Src, string DstId, string Dst);
    public record struct Endpoint(Guid Id, string Param);
    public record struct WireResult(int Index, bool Ok, Endpoint? Src, Endpoint? Dst, string? Error);
    public record struct BatchResult(int Count, int OkCount, WireResult[] Wires);

    [McpServerTool("g1_connect_many", "Connect GH1 Wires (Batch)", false, false)]
    [Description("Wire multiple output→input connections in one call. Same selector semantics as 'g1_connect' (numeric index or Name/NickName; '' or '0' for pure params). A failed wire does not stop later ones; per-wire results are returned. solve runs once at the end.")]
    public static IToolResult ConnectMany(
        RhinoDoc _,
        [Description("Array of {SrcId, Src, DstId, Dst} wire descriptors.")] WireSpec[] wires,
        [Description("If true (the default), sources already wired into a destination input before this call are removed first. Wires added within this same call accumulate. Pass false to add alongside everything.")] bool replace = true,
        [Description("If true, trigger a new solution after wiring. Set false to batch further.")] bool solve = true)
    {
        if (wires is null || wires.Length == 0)
            return Failure(
                ToolError.BadArgument,
                ContentBlock.CreateJson(new BatchResult(0, 0, [])),
                "No wires were supplied",
                "Pass at least one {SrcId, Src, DstId, Dst} entry");

        if (!GH1_Utils.TryGetDoc(out GH_Document doc))
            return GH1_Failures.NoDocument;

        WireResult[] results = new WireResult[wires.Length];
        Rewiring rewiring = new(replace);

        for (int i = 0; i < wires.Length; i++)
            results[i] = WireOne(doc, i, wires[i], rewiring);

        if (solve) doc.NewSolution(false);
        GH1_Utils.Redraw();

        int okCount = 0;
        for (int i = 0; i < results.Length; i++) if (results[i].Ok) okCount++;

        return Success(new BatchResult(wires.Length, okCount, results), rewiring.Guidance);
    }

    private static WireResult WireOne(GH_Document doc, int idx, WireSpec w, Rewiring rewiring)
    {
        if (!Guid.TryParse(w.SrcId, out Guid srcGuid))
            return new WireResult(idx, false, null, null, $"Invalid src_id '{w.SrcId}'");
        if (!Guid.TryParse(w.DstId, out Guid dstGuid))
            return new WireResult(idx, false, null, null, $"Invalid dst_id '{w.DstId}'");

        var srcObj = doc.FindObject(srcGuid, true);
        if (srcObj is null) return new WireResult(idx, false, null, null, $"Source '{srcGuid}' not found");
        var dstObj = doc.FindObject(dstGuid, true);
        if (dstObj is null) return new WireResult(idx, false, null, null, $"Destination '{dstGuid}' not found");

        if (ReferenceEquals(srcObj, dstObj))
            return new WireResult(idx, false, null, null, $"Object '{srcGuid}' cannot be wired to itself");

        if (!GH1_GraphOps.TryResolveOutput(srcObj, w.Src, out IGH_Param? srcParam, out string srcErr))
            return new WireResult(idx, false, null, null, srcErr);
        if (!GH1_GraphOps.TryResolveInput(dstObj, w.Dst, out IGH_Param? dstParam, out string dstErr))
            return new WireResult(idx, false, null, null, dstErr);

        try
        {
            rewiring.Note(GH1_GraphOps.Connect(srcParam!, dstParam!, rewiring.ShouldClear(dstParam!.InstanceGuid)));
        }
        catch (Exception ex)
        {
            return new WireResult(idx, false, null, null, ex.Message);
        }

        return new WireResult(
            idx, true,
            new Endpoint(srcObj.InstanceGuid, srcParam!.Name),
            new Endpoint(dstObj.InstanceGuid, dstParam!.Name),
            null);
    }
}
