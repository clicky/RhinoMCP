#if R9

using System.IO;
using System.Text;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using RhinoAI.ScriptProjects;
using RhinoAI.Tools;

namespace RhinoAI.ScriptProjects;

internal class RhinoCodeRunScript : IRhinoCodeRunner
{

    public IToolResult RunScript(RhinoDoc doc, Lang lang, string script)
    {
        if (lang == Lang.Python3)
            ScriptingEnvironment.EnsurePythonRuntimeIsAvailable();
        else if (lang == Lang.CSharp)
            ScriptingEnvironment.EnsureCSharpRuntimeIsAvailable();

        LanguageSpec spec = lang switch
        {
            Lang.CSharp => LanguageSpec.CSharp,
            Lang.Python3 => LanguageSpec.Python3,

            _ => throw new NotImplementedException("Unknown Language")
        };
        
        SourceCode source = new(spec, script);
        if (!source.TryCreateCode(out Code code))
        {
            string guidance = string.Join(", ", code.Diagnostics.Select(d => d.Message)); // TODO : Line/column
            return Failure(ToolError.Failed, "Could not create code from the supplied script.", guidance);
        }

        using MemoryStream output = new();
        using MemoryStream errors = new();
        RunContext context = new(defaultOutputStream: false, defaultErrorStream: false)
        {
            // Inserts __rhino_doc__ etc.
            AutoApplyParams = true,
            OutputStream = output,
            ErrorStream = errors,
        };
        
        context.Inputs["__rhino_doc__"] = doc;

        string? thrown = null;
        try
        {
            code.Run(context);
        }
        catch (Exception ex)
        {
            thrown = ex.Message;
        }

        string captured = Encoding.UTF8.GetString(errors.ToArray());

        if (captured.Length > 0 || thrown is not null)
        {
            return Failure(ToolError.Failed, [ContentBlock.CreateText(captured)], thrown ?? "The script wrote to stderr");
        }

        return Success(ContentBlock.CreateText(Encoding.UTF8.GetString(output.ToArray())));
    }
}

#endif
