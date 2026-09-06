using RhinoAI.Resources;

using Grasshopper.Kernel;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH1_ClearCanvasTool
{
    public record struct ClearResult(int Removed);

    [McpServerTool("g1_clear_canvas", "Clear GH1 Canvas", false, true)]
    [Description("Remove every object from the active GH1 canvas. Destructive — requires confirm=true.")]
    public static IToolResult Clear(
        RhinoDoc _,
        [Description("Must be true to actually wipe the canvas. Defaults to false as a safety guard.")] bool confirm = false,
        [Description("If true, trigger a new solution after clearing. Set false to batch multiple operations and solve once at the end.")] bool solve = true)
    {
        if (!confirm)
            return Failure(ToolError.Refused, "The canvas was not cleared", "Pass confirm=true to wipe the canvas");

        if (!GH1_Utils.TryGetDoc(out GH_Document doc))
            return GH1_Failures.NoDocument;

        List<IGH_DocumentObject> snapshot = doc.Objects.ToList();
        int count = snapshot.Count;

        doc.RemoveObjects(snapshot, false);
        if (solve) doc.NewSolution(true);
        GH1_Utils.Redraw();

        return Success(new ClearResult(count));
    }
}
