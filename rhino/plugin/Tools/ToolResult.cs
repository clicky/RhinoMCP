namespace RhinoAI.Tools;

internal interface IToolResult
{

    /// <summary>What kind of failure this is, or null when the call succeeded.</summary>
    public ToolError? Code { get; }

    /// <summary>Human-readable form of <see cref="Code"/>, or null when the call succeeded.</summary>
    public string? Error { get; }

    /// <summary>What specifically happened ("Layer not found: Walls").</summary>
    public string? Message { get; }

    /// <summary>What the agent should do next. Optional.</summary>
    public string? Guidance { get; }

    public IReadOnlyList<ContentBlock> Attachments { get; }

    public bool IsFailure { get; }

}

internal abstract record ToolResult : IToolResult
{

    public ToolError? Code { get; }

    public string? Error { get; }

    public string? Message { get; }

    public string? Guidance { get; }

    public IReadOnlyList<ContentBlock> Attachments { get; }

    public bool IsFailure => Code is not null;

    private ToolResult(IList<ContentBlock> attachments, ToolError? error = null, string? message = null, string? guidance = null)
    {
        Code = error;
        Error = ErrorMessage(error);
        Message = message;
        Guidance = guidance;
        Attachments = attachments.AsReadOnly();
    }

    public static IToolResult Failure(ToolError error, string? message = null, string? guidance = null)
        => new Err(error, [], message, guidance);

    public static IToolResult Failure(ToolError error, ContentBlock attachment, string? message = null, string? guidance = null)
        => new Err(error, [attachment], message, guidance);

    public static IToolResult Failure(ToolError error, IList<ContentBlock> attachments, string? message = null, string? guidance = null)
        => new Err(error, attachments, message, guidance);

    public static IToolResult Failure(Exception exception, string? guidance = null)
    {
        List<ContentBlock> blocks = [];
        if (exception.InnerException is not null)
        {
            blocks.Add(ContentBlock.CreateText(exception.InnerException.Message));
        }
        return new Err(ToolError.Exception, blocks, exception.Message, guidance);
    }

    public static IToolResult Success(params ContentBlock[] attachments)
        => new Ok(attachments);

    public static IToolResult Success(ContentBlock attachment, string? guidance = null)
        => new Ok([attachment], guidance);

    public static IToolResult Success<V>(V value, string? guidance = null)
        => new Ok([ContentBlock.CreateText(JsonSerializer.Serialize(value, McpSerializer.Options))], guidance);


    public sealed record Ok : ToolResult
    {
        public Ok(IList<ContentBlock> attachments, string? guidance = null) : base(attachments, null, null, guidance) { }
    }

    public sealed record Err : ToolResult
    {
        public Err(ToolError error, IList<ContentBlock> attachments, string? message = null, string? guidance = null)
            : base(attachments, error, message, guidance) { }
    }

    private static string? ErrorMessage(ToolError? err)
    => err switch
    {
        null => null,

        ToolError.NotFound => "Tool was not found",
        ToolError.BadArgument => "Tool received a Bad Argument",
        ToolError.Ambiguous => "Ambiguous",
        ToolError.Unsupported => "Unsupported",
        ToolError.Refused => "Refused",
        ToolError.Failed => "Failed",
        ToolError.Exception => "An Exception was thrown",

        // RH
        ToolError.RH_Doc_Headless => "This document is headless and has no viewport",
        ToolError.RH_View_NotFound => "There is no active view",
        ToolError.RH_Layer_NotFound => "Layer was not found",
        ToolError.RH_Object_NotFound => "Object was not found",
        ToolError.RH_Command_NotFound => "No matching Rhino command",
        ToolError.RH_File_NotFound => "File was not found",
        ToolError.RH_Nothing_Visible => "There is nothing to show",
        ToolError.RH_Write_Failed => "Could not write the file",

        // GH
        ToolError.GH_Document_NotFound => "Could not get or create GH document",
        ToolError.GH_NotAvailable => "Grasshopper is not available",
        ToolError.GH_Canvas_Empty => "The canvas has no active objects",
        ToolError.GH_Object_NotFound => "Canvas object was not found",
        ToolError.GH_Param_NotFound => "Parameter was not found",
        ToolError.GH_Component_NotFound => "Component was not found",
        ToolError.GH_Component_Failed => "Component could not be created",
        ToolError.GH_Solution_Failed => "The solution reported errors",

        _ => "Unknown Error",
    };

}

internal enum ToolError
{
    // Generic
    NotFound, BadArgument, Ambiguous, Unsupported, Refused, Failed, Exception,

    // Rhino
    RH_Doc_Headless,
    RH_View_NotFound,
    RH_Layer_NotFound,
    RH_Object_NotFound,
    RH_Command_NotFound,
    RH_File_NotFound,
    RH_Write_Failed,
    RH_Nothing_Visible,

    // GH
    GH_Document_NotFound,
    GH_NotAvailable,
    GH_Canvas_Empty,
    GH_Object_NotFound,
    GH_Param_NotFound,
    GH_Component_NotFound,
    GH_Component_Failed,
    GH_Solution_Failed,
}
