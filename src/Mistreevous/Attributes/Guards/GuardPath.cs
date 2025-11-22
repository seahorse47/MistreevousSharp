namespace Mistreevous;

/// <summary>
/// Represents a part of a guard path.
/// </summary>
public class GuardPathPart
{
    /// <summary>
    /// The node associated with this guard path part.
    /// </summary>
    public Node Node { get; set; } = null!;
    
    /// <summary>
    /// The guards associated with this guard path part.
    /// </summary>
    public List<Guard> Guards { get; set; } = new();
}

/// <summary>
/// Represents a path of node guards along a root-to-leaf tree path.
/// </summary>
public class GuardPath
{
    private readonly List<GuardPathPart> _nodes;

    /// <summary>
    /// Creates a new instance of the GuardPath class.
    /// </summary>
    /// <param name="nodes">An array of objects defining a node instance -> guard link, ordered by node depth.</param>
    public GuardPath(List<GuardPathPart> nodes)
    {
        _nodes = nodes;
    }

    /// <summary>
    /// Evaluate guard conditions for all guards in the tree path, moving outwards from the root.
    /// </summary>
    /// <param name="agent">The agent, required for guard evaluation.</param>
    public void Evaluate(IAgent agent)
    {
        // We need to evaluate guard conditions for nodes up the tree, moving outwards from the root.
        foreach (var details in _nodes)
        {
            // There can be multiple guards per node.
            foreach (var guard in details.Guards)
            {
                // Check whether the guard condition passes, and throw an exception if not.
                if (!guard.IsSatisfied(agent))
                {
                    throw new GuardUnsatisfiedException(details.Node, guard);
                }
            }
        }
    }
}

/// <summary>
/// Exception thrown when a guard is not satisfied.
/// </summary>
public class GuardUnsatisfiedException : Exception
{
    /// <summary>
    /// The node where the guard was not satisfied.
    /// </summary>
    public Node Node { get; }
    
    /// <summary>
    /// The guard that was not satisfied.
    /// </summary>
    public Guard Guard { get; }

    /// <summary>
    /// Creates a new instance of the GuardUnsatisfiedException class.
    /// </summary>
    /// <param name="node">The node where the guard was not satisfied.</param>
    /// <param name="guard">The guard that was not satisfied.</param>
    public GuardUnsatisfiedException(Node node, Guard guard)
        : base($"Guard '{guard.Condition}' is not satisfied for node '{node.GetName()}'")
    {
        Node = node;
        Guard = guard;
    }

    /// <summary>
    /// Gets whether this node is the source of the abort.
    /// </summary>
    public bool IsSourceNode(Node node)
    {
        return Node == node;
    }
}

