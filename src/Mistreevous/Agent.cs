namespace Mistreevous;

/// <summary>
/// A type representing an agent that a behavior tree instance would operate on.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Gets a property or method from the agent by name.
    /// </summary>
    object? this[string propertyName] { get; }
}

/// <summary>
/// Delegate for agent functions that can return a state or a task.
/// </summary>
public delegate object? AgentFunction(IAgent agent, params object?[] args);

/// <summary>
/// Delegate for global functions that can return a state or a task.
/// </summary>
public delegate object? GlobalFunction(IAgent agent, params object?[] args);

/// <summary>
/// Result type for action results.
/// </summary>
public enum ActionResult
{
    /// <summary>
    /// The action succeeded.
    /// </summary>
    Succeeded,
    
    /// <summary>
    /// The action failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The action is still running.
    /// </summary>
    Running
}

