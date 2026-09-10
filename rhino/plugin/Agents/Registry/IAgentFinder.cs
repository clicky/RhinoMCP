namespace Rhino.AI;

/// <summary>A resolver for agents to find them on the machine</summary>
internal interface IAgentFinder
{
    public List<string> Find();
}
