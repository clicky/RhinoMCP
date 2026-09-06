using RhinoAI.Resources;

using Grasshopper2.Doc;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH2_ClearCanvasTool
{
    public record struct ClearResult(int Removed);

    [McpServerTool("g2_clear_canvas", "Clear GH2 Canvas", false, true)]
    [Description("Remove every object from the active GH2 canvas. Destructive — requires confirm=true.")]
    public static IToolResult Clear(
        RhinoDoc rhDoc,
        [Description("Must be true to actually wipe the canvas. Defaults to false as a safety guard.")] bool confirm = false,
        [Description("If true, trigger a new solution after clearing.")] bool solve = true)
    {
        if (!confirm)
            return Failure(ToolError.Refused, "The canvas was not cleared", "Pass confirm=true to wipe the canvas");

        if (!GH2_Utils.TryGetDoc(rhDoc, out Document doc))
            return GH2_Failures.NoDocument;

        List<IDocumentObject> snapshot = doc.Objects.Forwards.ToList();
        int count = snapshot.Count;

        foreach (IDocumentObject obj in snapshot)
            doc.Objects.Remove(obj.InstanceId);

        if (solve)
            doc.Solution.Start();
        GH2_Utils.Redraw();

        return Success(new ClearResult(count));
    }
}
