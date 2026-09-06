using RhinoAI.Resources;

using Grasshopper;
using Grasshopper.Kernel;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class GH1_DescribeComponentTool
{
    public record struct ParamInfo(string Name, string NickName, string Description, string TypeName, string Access, bool Optional);

    public record struct ComponentInfo(
        string Name,
        string NickName,
        string Description,
        string Category,
        string SubCategory,
        string Kind,
        ParamInfo[] Inputs,
        ParamInfo[] Outputs);

    [McpServerTool("g1_describe_component", "Describe GH1 Component", true, false)]
    [Description("Look up a Grasshopper component by name and return its category, description, and input/output parameter list. Useful before placing or wiring components. Ignores obsolete/hidden unless includeDeprecated.")]
    public static IToolResult Describe(
        RhinoDoc _,
        [Description("Component name as it appears in the component library (e.g. 'Number Slider', 'Addition'). Case-insensitive.")] string name,
        [Description("Include obsolete/hidden components (e.g. legacy scripting). Default false.")] bool includeDeprecated = false) =>
        GH1_ProxyResolver.Resolve(name, includeDeprecated) switch
        {
            GH1_ProxyResolution.Found found => DescribeProxy(found.Proxy),
            GH1_ProxyResolution.Ambiguous ambiguous => Failure(
                ToolError.Ambiguous,
                ContentBlock.CreateJson(GH1_ProxyResolver.ToCandidates(ambiguous.Candidates)),
                $"'{name}' matches {ambiguous.Candidates.Count} components",
                GH1_ProxyResolver.AmbiguousMessage),
            GH1_ProxyResolution.OnlyDeprecated onlyDeprecated => Failure(
                ToolError.GH_Component_NotFound,
                ContentBlock.CreateJson(GH1_ProxyResolver.ToCandidates(onlyDeprecated.Candidates)),
                $"Only obsolete or hidden components match '{name}'",
                GH1_ProxyResolver.OnlyDeprecatedMessage),
            GH1_ProxyResolution.NotFound => Failure(ToolError.GH_Component_NotFound, $"No component named '{name}' found", "Find the right name with g1_search_components"),
            _ => throw new InvalidOperationException("Unhandled resolution case"),
        };

    private static IToolResult DescribeProxy(IGH_ObjectProxy proxy)
    {
        IGH_DocumentObject obj = proxy.CreateInstance();
        if (obj is null)
            return Failure(ToolError.GH_Component_Failed, $"Failed to instantiate '{proxy.Desc.Name}'");

        (string kind, ParamInfo[] inputs, ParamInfo[] outputs) = obj switch
        {
            IGH_Component comp => ("Component", comp.Params.Input.Select(ToInfo).ToArray(), comp.Params.Output.Select(ToInfo).ToArray()),
            IGH_Param param => ("Param", [ToInfo(param)], []),
            _ => (obj.GetType().Name, [], []),
        };

        ComponentInfo info = new(
            obj.Name,
            obj.NickName,
            obj.Description,
            obj.Category,
            obj.SubCategory,
            kind,
            inputs,
            outputs);

        return Success(info);
    }

    private static ParamInfo ToInfo(IGH_Param p) => new(
        p.Name,
        p.NickName,
        p.Description,
        p.TypeName,
        p.Access.ToString(),
        p.Optional);
}
