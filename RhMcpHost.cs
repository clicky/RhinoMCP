using Rhino;
using Rhino.PlugIns;

namespace RhMcp;

public static class RhMcpHost
{
    public const int DefaultPort = 4862;
    private const string PortKey = "port";

    private static PersistentSettings? _settings;
    private static McpServer? _server;

    public static int Port
    {
        get => _settings?.GetInteger(PortKey, DefaultPort) ?? DefaultPort;
        private set => _settings?.SetInteger(PortKey, value);
    }

    public static void Init(PersistentSettings settings)
    {
        _settings = settings;
        Start();
    }

    public static bool Start()
    {
        if (_server != null) return false;
        _server = new McpServer(Port);
        return _server.Start();
    }

    public static void Stop()
    {
        _server?.Stop();
        _server = null;
    }

    public static bool RestartOnPort(int port)
    {
        if (port < 1 || port > 65535) return false;
        Stop();
        Port = port;
        Start();
        return true;
    }
}
