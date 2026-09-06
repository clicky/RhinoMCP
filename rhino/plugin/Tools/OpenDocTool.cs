using Rhino.Display;
using Rhino.DocObjects;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class OpenDocTool
{
    [McpServerTool("open_doc", "Open / Import Document", false, true)]
    [Description("Import a .3dm (or other supported) file into the current document. Headless — no dialogs. Optionally clear the document first to make this behave like an open-in-place.")]
    public static IToolResult OpenDoc(
        RhinoDoc doc,
        [Description("Absolute path to the file to import")] string path,
        [Description("If true, delete all objects in the current document before importing")] bool clearFirst = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Failure(ToolError.BadArgument, "path is required.");

        if (!System.IO.File.Exists(path))
            return Failure(ToolError.RH_File_NotFound, $"File not found: {path}");

        int cleared = 0;
        if (clearFirst)
        {
            foreach (RhinoObject? obj in doc.Objects)
            {
                if (obj is null) continue;
                if (doc.Objects.Delete(obj.Id, true)) cleared++;
            }
        }

        int before = doc.Objects.Count;
        if (!doc.Import(path))
            return Failure(ToolError.Failed, $"Failed to import: {path}");
        int imported = doc.Objects.Count - before;

        foreach (RhinoView? view in doc.Views)
        {
            if (view is null) continue;
            view.ActiveViewport?.ZoomExtents();
        }

        doc.Views.Redraw(true);

        return Success(new
        {
            path,
            imported,
            cleared,
        });
    }
}
