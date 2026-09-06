using System.Drawing;
using System.IO;
using System.Reflection;

using Rhino.PlugIns;

namespace RhinoAI;

public class RhinoAIPlugin : PlugIn
{
    private const string IconResourceName = "RhinoAI.logo.svg";

    private CommandInterceptorHost? CommandInterceptors { get; set; }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhinoDoc.NewDocument += Register;
        RhinoDoc.EndOpenDocument += RegisterOpen;

        CommandInterceptors = new CommandInterceptorHost();

        // Probe agent install paths once on load so the active agent resolves before the first
        // prompt; Part 1's settings dialog re-runs this when the agent config changes.
        AgentRegistry.Refresh();

        Rhino.UI.Panels.RegisterPanel(this, typeof(AIPanel), "AI", LoadPanelIcon(), Rhino.UI.PanelType.PerDoc);
        return base.OnLoad(ref errorMessage);
    }

    // Adds the "AI" settings page to the Rhino Options dialog. Called each time Options is opened, so a
    // fresh page (and panel) is built per open and its state reflects the current settings.
    protected override void OptionsDialogPages(List<Rhino.UI.OptionsDialogPage> pages)
    {
        pages.Add(new AIOptionsPage());
    }

    // GetHicon isn't guaranteed on every platform, so fall back to no icon rather than fail OnLoad.
    private static System.Drawing.Icon? LoadPanelIcon()
    {
        try
        {
            Assembly assembly = typeof(RhinoAIPlugin).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(IconResourceName);
            if (stream is null)
                return null;

            using StreamReader reader = new(stream);
            string svg = reader.ReadToEnd();

            var size = Rhino.UI.Panels.IconSizeInPixels;
            int pixels = size.Width > 0 ? size.Width : 36;
            using System.Drawing.Bitmap bitmap = Rhino.UI.DrawingUtilities.BitmapFromSvg(svg, pixels, pixels, adjustForDarkMode: true);
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
        catch
        {
            return null;
        }
    }

    protected override void OnShutdown()
    {
        CommandInterceptors?.Dispose();
        AgentHost.Shutdown();
    }

    private void RegisterOpen(object? sender, DocumentOpenEventArgs e)
    {
        if (e.Merge) return;
        if (e.Reference) return;
        Register(sender, e);
    }

    private void Register(object? sender, DocumentEventArgs e)
    {   
        RhinoDoc.NewDocument -= Register;
        RhinoDoc.EndOpenDocument -= RegisterOpen;

        RhinoAIHost.RegisterDocumentWatcher();

        string? portStr = Environment.GetEnvironmentVariable(MCPSpawnCommand.PortEnvVar);
        if (!string.IsNullOrEmpty(portStr))
            return;

        if (!RhinoAIHost.TryGetNextPort(out int port))
        {
            RhinoApp.WriteLine("The Rhino MCP Server failed to start: no free port available.");
            return;
        }

        try
        {
            if (RhinoAIHost.StartOrRestart(e.Document, port, true))
            {
                RhinoApp.WriteLine("The Rhino MCP Platform is ready.");

                ScriptProjects.ScriptProjectStartup.ReloadWhenIdle();

                return;
            }
        }
        catch
        {
        }

        RhinoApp.WriteLine("The Rhino MCP Server failed to start");
    }

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

}
