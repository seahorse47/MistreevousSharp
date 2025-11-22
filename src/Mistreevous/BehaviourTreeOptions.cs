namespace Mistreevous;

/// <summary>
/// An object representing a change in state for a node in a behaviour tree instance.
/// </summary>
public class NodeStateChange
{
    /// <summary>
    /// The node unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The node type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The array of agent or globally registered function arguments if this is an action or condition node.
    /// </summary>
    public object?[]? Args { get; set; }

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
    /// The previous state of the node.
    /// </summary>
    public State PreviousState { get; set; }

    /// <summary>
    /// The current state of the node.
    /// </summary>
    public State State { get; set; }
}

/// <summary>
/// The options object that can be passed as an argument when instantiating the BehaviourTree class.
/// </summary>
public class BehaviourTreeOptions
{
    /// <summary>
    /// Gets a delta time in seconds that is used to calculate the elapsed duration of any `wait` nodes.
    /// If this function is not defined then `DateTime.UtcNow` will be used instead by default.
    /// </summary>
    public Func<double>? GetDeltaTime { get; set; }

    /// <summary>
    /// Gets a pseudo-random floating-point number between 0 (inclusive) and 1 (exclusive) for use in operations such as:
    /// - The selection of active children for any `lotto` nodes.
    /// - The selection of durations for `wait` nodes,
    /// - The selection of iterations for `repeat` nodes and attempts for `retry` nodes when minimum and maximum bounds are defined.
    /// If not defined then `Random.NextDouble()` will be used instead by default.
    /// </summary>
    public Func<double>? Random { get; set; }

    /// <summary>
    /// An event handler that is called whenever the state of a node changes.
    /// </summary>
    public Action<NodeStateChange>? OnNodeStateChange { get; set; }
}

