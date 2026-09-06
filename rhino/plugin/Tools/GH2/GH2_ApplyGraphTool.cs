using System.Threading.Tasks;

using RhinoAI.Resources;

using Eto.Drawing;

using Grasshopper2.Doc;
using Grasshopper2.Framework;
using Grasshopper2.Parameters;
using Grasshopper2.Parameters.Special;
using Grasshopper2.UI;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class GH2_ApplyGraphTool
{
    public record struct ComponentSpec(string Key, string Selector, float X, float Y);
    public record struct SliderSpec(string Key, double Min, double Value, double Max, int Decimals, string? Name, float X, float Y);
    public record struct WireSpec(string SrcKey, string Src, string DstKey, string Dst);

    public record struct PlacedRef(string Key, Guid Id, string Kind);
    public record struct PlaceError(string Key, string Error);
    public record struct WireResult(int Index, bool Ok, string? Error);

    public record struct ApplyResult(
        PlacedRef[] Placed,
        PlaceError[] PlaceErrors,
        WireResult[] Wires,
        int WiresOk,
        GH2SolveSummary? Solve);

    [McpServerTool("g2_apply_graph", "Apply GH2 Graph", false, false)]
    [Description("Place sliders + components and wire them in one call on the active GH2 canvas. References between objects use caller-supplied 'key' strings; the tool returns the key→Guid map. Failures in any step do not abort the rest; results report per-step status. Wire src/dst use the same selector semantics as 'g2_connect'. When solve=true (the default) it also solves at the end and reads the result back, returning the same solve summary as g2_solve_canvas: {Solved, Phase, Errors, Warnings, Diagnostics[]}, where each diagnostic is {Id, Name, Nickname, Level (Remark|Warning|Error|Fault), Message}. Solved is true only when the solution completed with no Error or Fault; read the Diagnostics back to see which components failed and why.")]
    public static async Task<IToolResult> Apply(
        RhinoDoc rhDoc,
        [Description("Sliders to place: {Key, Min, Value, Max, Decimals, Name?, X, Y}. Decimals: 0..12.")] SliderSpec[] sliders,
        [Description("Components to place: {Key, Selector, X, Y}. Selector is a Guid (preferred) or component Name.")] ComponentSpec[] components,
        [Description("Wires to create: {SrcKey, Src, DstKey, Dst}. Keys must match a slider or component key above.")] WireSpec[] wires,
        [Description("If true, trigger a new solution at the end.")] bool solve = true,
        [Description("If true (the default), sources already wired into a destination input before this call are removed first. Wires added within this same call accumulate. Pass false to add alongside everything.")] bool replace = true,
        [Description("Also match obsolete/hidden components by name (a Guid always works). Default false.")] bool includeDeprecated = false)
    {
        if (!GH2_Utils.TryGetDoc(rhDoc, out Document doc))
            return GH2_Failures.NoDocument;

        Coercions coerced = new();
        Rewiring rewiring = new(replace);
        var keyToObj = new Dictionary<string, IDocumentObject>(StringComparer.Ordinal);
        var placed = new List<PlacedRef>();
        var placeErrors = new List<PlaceError>();
        var wireResults = new WireResult[wires?.Length ?? 0];

        if (sliders is not null)
        {
            foreach (var s in sliders)
            {
                if (keyToObj.ContainsKey(s.Key))
                {
                    placeErrors.Add(new PlaceError(s.Key, "duplicate key"));
                }
                else if (TryPlaceSlider(doc, s, coerced, out var slider, out var err))
                {
                    keyToObj[s.Key] = slider!;
                    placed.Add(new PlacedRef(s.Key, slider!.InstanceId, "Slider"));
                }
                else
                {
                    placeErrors.Add(new PlaceError(s.Key, err));
                }
            }
        }

        if (components is not null)
        {
            foreach (var c in components)
            {
                if (keyToObj.ContainsKey(c.Key))
                {
                    placeErrors.Add(new PlaceError(c.Key, "duplicate key"));
                }
                else if (TryPlaceComponent(doc, c, includeDeprecated, out var obj, out var err))
                {
                    keyToObj[c.Key] = obj!;
                    placed.Add(new PlacedRef(c.Key, obj!.InstanceId, GH2_Utils.ClassifyKind(obj.GetType())));
                }
                else
                {
                    placeErrors.Add(new PlaceError(c.Key, err));
                }
            }
        }

        if (wires is not null)
        {
            for (int i = 0; i < wires.Length; i++)
                wireResults[i] = WireOne(doc, i, wires[i], keyToObj, rewiring);
        }

        // A solver throw still returns the partial work rather than losing the placing and wiring already done.
        GH2SolveSummary? summary = solve ? await GH2_Diagnostics.SolveAsync(doc) : null;
        GH2_Utils.Redraw();

        int wiresOk = 0;
        for (int i = 0; i < wireResults.Length; i++) if (wireResults[i].Ok) wiresOk++;

        if (rewiring.Guidance is string rewired)
            coerced.Note(rewired);

        return Success(
            new ApplyResult(
                placed.ToArray(),
                placeErrors.ToArray(),
                wireResults,
                wiresOk,
                summary),
            coerced.Guidance);
    }

    private static bool TryPlaceSlider(Document doc, SliderSpec s, Coercions coerced, out NumberSliderObject? slider, out string error)
    {
        slider = null;
        int decimals = coerced.Clamp($"slider '{s.Key}' decimals", s.Decimals, 0, 12);
        (double min, double value, double max) = coerced.SliderRange(s.Min, s.Value, s.Max);

        var number = new UiNumber(decimals, (decimal)value, (decimal)min, (decimal)max);
        slider = new NumberSliderObject(string.IsNullOrEmpty(s.Name) ? "num" : s.Name!, number);
        doc.Objects.Add(slider, new PointF(s.X, s.Y));
        error = "";
        return true;
    }

    private static bool TryPlaceComponent(Document doc, ComponentSpec c, bool includeDeprecated, out IDocumentObject? obj, out string error)
    {
        obj = null;
        if (Guid.TryParse(c.Selector, out Guid guid))
        {
            var proxy = ObjectProxies.FindById(guid);
            if (proxy is null) { error = $"No component with guid '{guid}'"; return false; }
            obj = proxy.Emit();
            if (obj is null) { error = $"Failed to emit '{guid}'"; return false; }
        }
        else
        {
            switch (GH2_ProxyResolver.Resolve(c.Selector, includeDeprecated))
            {
                case GH2_ProxyResolution.Found found:
                    obj = found.Proxy.Emit();
                    if (obj is null) { error = $"Failed to instantiate '{c.Selector}'"; return false; }
                    break;
                case GH2_ProxyResolution.Ambiguous ambiguous:
                    error = $"Component name '{c.Selector}' is ambiguous ({ambiguous.Candidates.Count} matches): {Summarize(ambiguous.Candidates)}. Pass a Guid to disambiguate.";
                    return false;
                case GH2_ProxyResolution.OnlyDeprecated onlyDeprecated:
                    error = $"Only obsolete or hidden components match '{c.Selector}': {Summarize(onlyDeprecated.Candidates)}. Pass a Guid, or set includeDeprecated=true, to use one.";
                    return false;
                case GH2_ProxyResolution.NotFound:
                    error = $"No component named '{c.Selector}'";
                    return false;
                default:
                    throw new InvalidOperationException("Unhandled resolution case");
            }
        }
        doc.Objects.Add(obj, new PointF(c.X, c.Y));
        error = "";
        return true;
    }

    private static string Summarize(IReadOnlyList<ObjectProxy> proxies) =>
        string.Join(", ", proxies.Select(p => $"{p.Id} ({p.Nomen.Chapter}/{p.Nomen.Section})"));

    private static WireResult WireOne(Document doc, int idx, WireSpec w, Dictionary<string, IDocumentObject> keyToObj, Rewiring rewiring)
    {
        if (!keyToObj.TryGetValue(w.SrcKey, out var srcObj))
            return new WireResult(idx, false, $"src_key '{w.SrcKey}' did not match a placed object");
        if (!keyToObj.TryGetValue(w.DstKey, out var dstObj))
            return new WireResult(idx, false, $"dst_key '{w.DstKey}' did not match a placed object");

        if (GH2_GraphOps.WouldCycle(doc, srcObj, dstObj))
            return new WireResult(idx, false, $"wiring key '{w.SrcKey}' into '{w.DstKey}' would create a cycle");

        if (!GH2_GraphOps.TryResolveOutput(srcObj, w.Src, out IParameter? srcParam, out string srcErr))
            return new WireResult(idx, false, srcErr);
        if (!GH2_GraphOps.TryResolveInput(dstObj, w.Dst, out IParameter? dstParam, out string dstErr))
            return new WireResult(idx, false, dstErr);

        try
        {
            rewiring.Note(GH2_GraphOps.Connect(srcParam!, dstParam!, rewiring.ShouldClear(dstParam!.InstanceId)));
        }
        catch (Exception ex)
        {
            return new WireResult(idx, false, ex.Message);
        }

        return new WireResult(idx, true, null);
    }
}
