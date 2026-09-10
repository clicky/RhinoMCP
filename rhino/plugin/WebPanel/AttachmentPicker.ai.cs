using System.IO;

using Eto.Forms;

namespace Rhino.AI.WebPanel;

// Picking lives here, not in the page: a WKWebView has no open panel, so <input type="file"> does nothing on macOS.
internal static class AttachmentPicker
{
    private const long MaxBytes = 10 * 1024 * 1024;

    private static Dictionary<string, string> ImageTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    private static string[] TextExtensions { get; } =
        [".txt", ".md", ".json", ".csv", ".xml", ".yml", ".yaml", ".log", ".py", ".cs", ".js", ".ts", ".html", ".css", ".sql"];

    public static IReadOnlyList<PanelAttachment> Pick(Control parent, Action<string> warn)
    {
        OpenFileDialog dialog = new() { Title = "Attach files", MultiSelect = true };
        dialog.Filters.Add(new FileFilter("Images and text", [.. ImageTypes.Keys, .. TextExtensions]));
        dialog.Filters.Add(new FileFilter("Images", [.. ImageTypes.Keys]));
        dialog.Filters.Add(new FileFilter("All files", ".*"));

        if (dialog.ShowDialog(parent) != DialogResult.Ok)
            return [];

        List<PanelAttachment> picked = [];
        foreach (string path in dialog.Filenames)
            if (Read(path, warn) is { } attachment)
                picked.Add(attachment);
        return picked;
    }

    private static PanelAttachment? Read(string path, Action<string> warn)
    {
        string name = Path.GetFileName(path);
        try
        {
            long length = new FileInfo(path).Length;
            if (length > MaxBytes)
            {
                warn($"{name} is {length / (1024 * 1024)} MB, over the {MaxBytes / (1024 * 1024)} MB attachment limit.");
                return null;
            }

            byte[] data = File.ReadAllBytes(path);
            if (ImageTypes.TryGetValue(Path.GetExtension(path), out string? mediaType))
                return PanelAttachment.From(NextId(), AttachmentKind.Image, name, mediaType, data);

            if (!PanelAttachment.LooksLikeText(data))
            {
                warn($"{name} is neither an image nor a text file, so it cannot be attached.");
                return null;
            }
            return PanelAttachment.From(NextId(), AttachmentKind.TextFile, name, "text/plain", data);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            warn($"Could not read {name}: {ex.Message}");
            return null;
        }
    }

    private static string NextId() => $"host-{Guid.NewGuid():N}";
}
