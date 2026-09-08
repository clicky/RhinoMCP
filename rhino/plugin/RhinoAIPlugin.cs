using System.IO;
using System.Reflection;

using Rhino.PlugIns;
using Rhino.Runtime;

namespace RhinoAI;

public class RhinoAIPlugin : PlugIn
{
    private const string IconResourceName = "RhinoAI.logo.ico";
    private const string DarkIconResourceName = "RhinoAI.logo-dark.ico";

    private CommandInterceptorHost? CommandInterceptors { get; set; }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        if (AIAutoLoad.ShouldAutoLoad())
        {
            RhinoDoc.NewDocument += Register;
            RhinoDoc.EndOpenDocument += RegisterOpen;

            CommandInterceptors = new CommandInterceptorHost();
        }

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

            string resourceName = HostUtils.RunningInDarkMode ? DarkIconResourceName : IconResourceName;
            using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is null)
                return null;

            return new System.Drawing.Icon(resourceStream);
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
            RhinoApp.WriteLine("RhinoAI's MCP server failed to start: no free port available.");
            return;
        }

        try
        {
            if (RhinoAIHost.StartOrRestart(e.Document, port, true))
            {
                ScriptProjects.ScriptProjectStartup.ReloadWhenIdle();

                return;
            }
        }
        catch
        {
        }

        RhinoApp.WriteLine("RhinoAI's MCP Server failed to start");
    }

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

}
