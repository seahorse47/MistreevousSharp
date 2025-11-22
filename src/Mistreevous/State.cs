namespace Mistreevous;

/// <summary>
/// Enumeration of node state types.
/// </summary>
public enum State
{
    /// <summary>
    /// The state that a node will be in when it has not been visited yet in the execution of the tree.
    /// </summary>
    Ready,

    /// <summary>
    /// The state that a node will be in when it is still being processed and will usually represent or encompass a long-running action.
    /// </summary>
    Running,

    /// <summary>
    /// The state that a node will be in when it is no longer being processed and has succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The state that a node will be in when it is no longer being processed but has failed.
    /// </summary>
    Failed
}

