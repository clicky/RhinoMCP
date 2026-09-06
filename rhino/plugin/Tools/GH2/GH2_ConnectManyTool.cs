using System.Threading.Tasks;

using RhinoAI.Resources;

using Grasshopper2.Doc;
using Grasshopper2.Parameters;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH2_ConnectManyTool
{
    public record struct WireSpec(string SrcId, string Src, string DstId, string Dst);
    public record struct Endpoint(Guid Id, string Param);
    public record struct WireResult(int Index, bool Ok, Endpoint? Src, Endpoint? Dst, string? Error);
    public record struct BatchResult(int Count, int OkCount, WireResult[] Wires, GH2SolveSummary? Solve);

    [McpServerTool("g2_connect_many", "Connect GH2 Wires (Batch)", false, false)]
    [Description("Wire multiple output→input connections in one call on the active GH2 canvas. Same selector semantics as 'g2_connect'. A failed wire does not stop later ones; per-wire results are returned. solve runs once at the end.")]
    public static async Task<IToolResult> ConnectMany(
        RhinoDoc rhDoc,
        [Description("Array of {SrcId, Src, DstId, Dst} wire descriptors.")] WireSpec[] wires,
        [Description("If true (the default), sources already wired into a destination input before this call are removed first. Wires added within this same call accumulate. Pass false to add alongside everything.")] bool replace = true,
        [Description("If true, trigger a new solution after wiring. Set false to batch further.")] bool solve = true)
    {
        if (wires is null || wires.Length == 0)
            return Failure(
                ToolError.BadArgument,
                ContentBlock.CreateJson(new BatchResult(0, 0, [], null)),
                "No wires were supplied",
                "Pass at least one {SrcId, Src, DstId, Dst} entry");

        if (!GH2_Utils.TryGetDoc(rhDoc, out Document doc))
            return GH2_Failures.NoDocument;

        WireResult[] results = new WireResult[wires.Length];
        Rewiring rewiring = new(replace);

        for (int i = 0; i < wires.Length; i++)
            results[i] = WireOne(doc, i, wires[i], rewiring);

        GH2SolveSummary? summary = solve ? await GH2_Diagnostics.SolveAsync(doc) : null;
        GH2_Utils.Redraw();

        int okCount = 0;
        for (int i = 0; i < results.Length; i++)
            if (results[i].Ok)
                okCount++;

        return Success(new BatchResult(wires.Length, okCount, results, summary), rewiring.Guidance);
    }

    private static WireResult WireOne(Document doc, int idx, WireSpec w, Rewiring rewiring)
    {
        if (!Guid.TryParse(w.SrcId, out Guid srcGuid))
            return new WireResult(idx, false, null, null, $"Invalid src_id '{w.SrcId}'");
        if (!Guid.TryParse(w.DstId, out Guid dstGuid))
            return new WireResult(idx, false, null, null, $"Invalid dst_id '{w.DstId}'");

        IDocumentObject srcObj = doc.Objects.Find(srcGuid);
        if (srcObj is null) return new WireResult(idx, false, null, null, $"Source '{srcGuid}' not found");
        IDocumentObject dstObj = doc.Objects.Find(dstGuid);
        if (dstObj is null) return new WireResult(idx, false, null, null, $"Destination '{dstGuid}' not found");

        if (ReferenceEquals(srcObj, dstObj))
            return new WireResult(idx, false, null, null, $"Object '{srcGuid}' cannot be wired to itself");

        if (!GH2_GraphOps.TryResolveOutput(srcObj, w.Src, out IParameter? srcParam, out string srcErr))
            return new WireResult(idx, false, null, null, srcErr);
        if (!GH2_GraphOps.TryResolveInput(dstObj, w.Dst, out IParameter? dstParam, out string dstErr))
            return new WireResult(idx, false, null, null, dstErr);

        try
        {
            rewiring.Note(GH2_GraphOps.Connect(srcParam!, dstParam!, rewiring.ShouldClear(dstParam!.InstanceId)));
        }
        catch (Exception ex)
        {
            return new WireResult(idx, false, null, null, ex.Message);
        }

        return new WireResult(
            idx, true,
            new Endpoint(srcObj.InstanceId, srcParam!.Nomen.Name),
            new Endpoint(dstObj.InstanceId, dstParam!.Nomen.Name),
            null);
    }
}
