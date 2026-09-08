
namespace RhinoAI;

/// <summary>
/// Many users don't need AI, or MCP, and spawning a small local http server "because" is bad practice.
/// </summary>
internal static class AIAutoLoad
{

    public static bool ShouldAutoLoad()
    {
        AgentRegistry.Refresh();
        foreach(ResolvedAgent? agent in AgentRegistry.Chain)
        {
            if (agent is null) continue;
            if (!agent.Available) continue;
            return true;
        }
        
        return false;
    }

}
