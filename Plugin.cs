using Rhino;
using Rhino.PlugIns;

namespace RhMcp;

public class RhMcpPlugin : PlugIn
{

#if DEBUG
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
#endif

    public RhMcpPlugin()
    {
        Instance = this;
    }

    #pragma warning disable
    public static RhMcpPlugin Instance { get; private set; }
    #pragma warning enable

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhMcpHost.Init(Settings);
        return LoadReturnCode.Success;
    }

    protected override void OnShutdown() => RhMcpHost.Stop();
}
