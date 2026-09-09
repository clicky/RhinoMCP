using System.IO;

using Rhino.FileIO;

namespace Rhino.AI.Tools;

[McpServerToolType]
internal static class SaveDocTool
{
    private const string Extension = ".3dm";

    [McpServerTool("save_doc", "Save Document", false, true)]
    [Description("Write the current document to the given .3dm path. Headless — no dialogs. Overwrites any existing file at the path.")]
    public static IToolResult SaveDoc(
        RhinoDoc doc,
        [Description("Absolute path to write the .3dm file to")] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Failure(ToolError.BadArgument, "path is required.");

        path = path.Trim();

        if (!Path.IsPathRooted(path))
            return Failure(ToolError.BadArgument, $"path must be absolute: {path}");

        if (string.IsNullOrEmpty(Path.GetFileName(path)))
            return Failure(ToolError.BadArgument, $"path must include a file name: {path}");

        Coercions coerced = new();

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path += Extension;
            coerced.Note($"path did not have an extension, so it was saved as {path}");
        }

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            if (!Directory.CreateDirectory(dir).Exists)
            {
                return Failure(ToolError.RH_File_NotFound, $"Directory does not exist: {dir}");
            }
        }

        // UpdateDocumentPath=false: avoids the post-write LockDocument that pops a modal in R9.
        FileWriteOptions options = new()
        {
            SuppressDialogBoxes = true,
            WriteUserData = true,
            UpdateDocumentPath = false,
            SuppressAllInput = true,
        };

        if (string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
        {
            if (!doc.WriteFile(path, options))
                return Failure(ToolError.RH_Write_Failed, $"Failed to save: {path}");
        }
        // Non-3dm extension - Export
        else
        {
            if (!doc.Export(path))
                return Failure(ToolError.RH_Write_Failed, $"Failed to save: {path}");
        }

        return Success(new
        {
            path,
            objects = doc.Objects.Count,
        }, coerced.Guidance);
    }
}
