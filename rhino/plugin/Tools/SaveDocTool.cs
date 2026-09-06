using Rhino.FileIO;

namespace RhinoAI.Tools;

// TODO : This needs some tweaking

[McpServerToolType]
public static class SaveDocTool
{
    [McpServerTool("save_doc", "Save Document", false, true)]
    [Description("Write the current document to the given .3dm path. Headless — no dialogs. Overwrites any existing file at the path.")]
    public static IToolResult SaveDoc(
        RhinoDoc doc,
        [Description("Absolute path to write the .3dm file to")] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Failure(ToolError.BadArgument, "path is required.");

        string? dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            return Failure(ToolError.RH_File_NotFound, $"Directory does not exist: {dir}");

        // UpdateDocumentPath=false: avoids the post-write LockDocument that pops a modal in R9.
        FileWriteOptions options = new ()
        {
            SuppressDialogBoxes = true,
            WriteUserData = true,
            UpdateDocumentPath = false,
        };

        if (!doc.WriteFile(path, options))
            return Failure(ToolError.RH_Write_Failed, $"Failed to save: {path}");

        return Success(new
        {
            path,
            objects = doc.Objects.Count,
        });
    }
}
