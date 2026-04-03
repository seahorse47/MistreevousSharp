namespace Mistreevous;

/// <summary>
/// Details of a tree node instance.
/// </summary>
public class NodeDetails
{
    /// <summary>
    /// The tree node identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The tree node type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The tree node name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The current state of the tree node.
    /// </summary>
    public State State { get; set; }

    /// <summary>
    /// The agent or globally registered function arguments, defined if this is an action or condition node.
    /// </summary>
    public Arguments? Args { get; set; }

    /// <summary>
    /// The 'while' guard attribute configured for this node.
    /// </summary>
    public GuardAttributeDetails? While { get; set; }

    /// <summary>
    /// The 'until' guard attribute configured for this node.
    /// </summary>
    public GuardAttributeDetails? Until { get; set; }

    /// <summary>
    /// The 'entry' callback attribute configured for this node.
    /// </summary>
    public CallbackAttributeDetails? Entry { get; set; }

    /// <summary>
    /// The 'step' callback attribute configured for this node.
    /// </summary>
    public CallbackAttributeDetails? Step { get; set; }

    /// <summary>
    /// The 'exit' callback attribute configured for this node.
    /// </summary>
    public CallbackAttributeDetails? Exit { get; set; }

    /// <summary>
    /// The array of the child nodes of this node, defined if this node is a composite or decorator node.
    /// </summary>
    public List<NodeDetails>? Children { get; set; }
}

/// <summary>
/// Details of a guard attribute.
/// </summary>
public class GuardAttributeDetails
{
    /// <summary>
    /// Gets or sets the name of the guard function to call.
    /// </summary>
    public string Call { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the arguments to pass to the guard function.
    /// </summary>
    public Arguments? Args { get; set; }
    
    /// <summary>
    /// Gets or sets whether the node should succeed when aborted by this guard.
    /// </summary>
    public bool SucceedOnAbort { get; set; }
}

/// <summary>
/// Details of a callback attribute.
/// </summary>
public class CallbackAttributeDetails
{
    /// <summary>
    /// Gets or sets the name of the callback function to call.
    /// </summary>
    public string Call { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the arguments to pass to the callback function.
    /// </summary>
    public Arguments? Args { get; set; }
}

