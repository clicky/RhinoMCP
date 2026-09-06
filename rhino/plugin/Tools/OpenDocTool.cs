using Rhino.Display;
using Rhino.DocObjects;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class OpenDocTool
{
    [McpServerTool("open_doc", "Open / Import Document", false, true)]
    [Description("Import a .3dm (or other supported) file into the current document. Headless — no dialogs. Optionally clear the document first to make this behave like an open-in-place.")]
    public static string OpenDoc(
        RhinoDoc doc,
        [Description("Absolute path to the file to import")] string path,
        [Description("If true, delete all objects in the current document before importing")] bool clearFirst = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is required.", nameof(path));

        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException($"File not found: {path}", path);

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
            throw new InvalidOperationException($"Failed to import: {path}");
        int imported = doc.Objects.Count - before;

        foreach (RhinoView? view in doc.Views)
        {
            if (view is null) continue;
            view.ActiveViewport.ZoomExtents();
        }

        doc.Views.Redraw();

        return JsonSerializer.Serialize(new
        {
            path,
            imported,
            cleared,
        });
    }
}
