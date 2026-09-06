using Grasshopper2;
using Grasshopper2.Doc;
using Grasshopper2.Framework;
using Grasshopper2.Parameters;
using Grasshopper2.Parameters.Special;
using Grasshopper2.UI;
using Grasshopper2.UI.Canvas;

using GH2Component = Grasshopper2.Components.Component;

namespace RhinoAI.Resources;

internal static class GH2_Utils
{

  public static bool TryGetDoc(RhinoDoc rhDoc, out Document doc)
  {
    doc = default!;

    Editor? editor = Editor.Instance;
    if (editor is null)
    {
        string commandName = Rhino.Commands.Command.IsCommand("_G2") ? "_G2" : "_GH2";
        RhinoApp.RunScript(rhDoc.RuntimeSerialNumber, commandName, true);

        // Re-read: the launch above is what populates it, so the local captured before it is always null.
        editor = Editor.Instance;
        if (editor is null)
            return false;
    }

    doc = editor.Canvas?.Document!;

    return doc is not null;
  }

  // Read-only probe for callers that must never put GH2 on screen, unlike TryGetDoc, which launches it.
  public static bool TryPeekDoc(out Document doc)
  {
    doc = Editor.Instance?.Canvas?.Document!;
    return doc is not null;
  }

  public static bool TryLoadDocument(RhinoDoc rhDoc, string path)
  {
    if (!TryGetDoc(rhDoc, out _)) return false;
    return Editor.Instance.Documents.TryOpenDocument(path, OpenDocumentOptions.Default);
  }

  // Marshalled because the solve-capable tools now await, and their continuation may land off the UI thread.
  public static void Redraw()
  {
    RhinoApp.InvokeOnUiThread(new Action(() =>
    {
      Canvas? canvas = Editor.Instance?.Canvas;
      canvas?.Invalidate();
    }));
  }

  public static string ClassifyKind(Type t)
  {
    if (t is null) return "Other";
    if (typeof(NumberSliderObject).IsAssignableFrom(t)) return "Slider";
    if (typeof(GH2Component).IsAssignableFrom(t)) return "Component";
    if (typeof(IParameter).IsAssignableFrom(t)) return "Param";
    return "Other";
  }

  public static bool IsValueSource(IDocumentObject obj) =>
    obj.GetType().Namespace?.Contains("Parameters.Special") ?? false;

}
