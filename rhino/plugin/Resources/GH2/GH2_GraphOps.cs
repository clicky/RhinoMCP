using Grasshopper2.Doc;
using Grasshopper2.Parameters;

using GH2Component = Grasshopper2.Components.Component;

namespace RhinoAI.Resources;

internal static class GH2_GraphOps
{
    // Wiring src into dst closes a loop when src already depends on dst, so the walk goes upstream from src looking for dst.
    public static bool WouldCycle(Document doc, IDocumentObject src, IDocumentObject dst)
    {
        HashSet<Guid> seen = [];
        Queue<IDocumentObject> pending = new();
        pending.Enqueue(src);

        while (pending.Count > 0)
        {
            IDocumentObject current = pending.Dequeue();
            if (current.InstanceId == dst.InstanceId)
                return true;

            if (!seen.Add(current.InstanceId))
                continue;

            foreach (IParameter input in InputsOf(current))
            {
                foreach (Guid sourceId in input.Inputs.Forwards)
                {
                    IParameter? source = doc.Objects.FindParameter(sourceId);
                    if (source is null)
                        continue;

                    pending.Enqueue(source.FoundingObject ?? source);
                }
            }
        }

        return false;
    }

    private static IEnumerable<IParameter> InputsOf(IDocumentObject obj) => obj switch
    {
        GH2Component comp => comp.Parameters.Inputs,
        IParameter param => [param],
        _ => [],
    };

    public static int Connect(IParameter src, IParameter dst, bool replace)
    {
        bool wired = dst.Inputs.IndexOf(src.InstanceId) >= 0;
        int removed = 0;

        if (replace)
        {
            removed = dst.Inputs.Count - (wired ? 1 : 0);
            if (removed > 0)
                Connections.DisconnectAllInputsExcept(dst, src.InstanceId);
        }

        if (!wired)
            Connections.Connect(src, dst);

        return removed;
    }

    public static bool TryResolveOutput(IDocumentObject obj, string selector, out IParameter? param, out string error)
    {
        param = null;
        error = "";
        if (obj is GH2Component comp)
            return TryPickParam(comp.Parameters.Outputs.ToList(), selector, "output", out param, out error);
        if (obj is IParameter p)
        {
            if (selector is null || selector.Length == 0 || selector == "0") { param = p; return true; }
            error = $"Object '{obj.InstanceId}' is a Param; expected '' or '0' for src, got '{selector}'";
            return false;
        }
        error = $"Object '{obj.InstanceId}' has no outputs";
        return false;
    }

    public static bool TryResolveInput(IDocumentObject obj, string selector, out IParameter? param, out string error)
    {
        param = null;
        error = "";
        if (obj is GH2Component comp)
            return TryPickParam(comp.Parameters.Inputs.ToList(), selector, "input", out param, out error);
        if (obj is IParameter p)
        {
            if (GH2_Utils.IsValueSource(p))
            {
                error = $"destination '{p.DisplayName}' is a value source, not an input";
                return false;
            }
            if (selector is null || selector.Length == 0 || selector == "0") { param = p; return true; }
            error = $"Object '{obj.InstanceId}' is a Param; expected '' or '0' for dst, got '{selector}'";
            return false;
        }
        error = $"Object '{obj.InstanceId}' has no inputs";
        return false;
    }

    private static bool TryPickParam(IList<IParameter> list, string selector, string kind, out IParameter? param, out string error)
    {
        param = null;
        error = "";

        if (selector is null || selector.Length == 0)
        {
            if (list.Count == 0) { error = $"Component has no {kind} params"; return false; }
            param = list[0];
            return true;
        }

        if (int.TryParse(selector, out int idx))
        {
            if (idx < 0 || idx >= list.Count) { error = $"{kind} index {idx} out of range (count {list.Count})"; return false; }
            param = list[idx];
            return true;
        }

        foreach (var p in list)
            if (string.Equals(p.Nomen.Name, selector, StringComparison.OrdinalIgnoreCase)) { param = p; return true; }
        foreach (var p in list)
            if (string.Equals(p.UserName, selector, StringComparison.OrdinalIgnoreCase)) { param = p; return true; }
        foreach (var p in list)
            if (string.Equals(p.DisplayName, selector, StringComparison.OrdinalIgnoreCase)) { param = p; return true; }

        error = $"No {kind} named '{selector}'";
        return false;
    }
}
