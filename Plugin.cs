using Rhino.PlugIns;

namespace RhMcp;

public class RhMcpPlugin : PlugIn
{
#if DEBUG
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
#endif

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhMcpHost.Init(Settings);
        return LoadReturnCode.Success;
    }

    protected override void OnShutdown() => RhMcpHost.Stop();
}
