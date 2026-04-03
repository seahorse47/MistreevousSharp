namespace Mistreevous;

/// <summary>
/// A composite node that wraps child nodes.
/// </summary>
public abstract class Composite : Node
{
    /// <summary>
    /// The child nodes.
    /// </summary>
    protected readonly List<Node> Children;

    /// <summary>
    /// Creates a new instance of the Composite class.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    protected Composite(string type, List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base(type, attributes, options)
    {
        Children = children;
    }

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public List<Node> GetChildren() => Children;

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public override void Reset()
    {
        // Reset the state of this node.
        SetState(State.Ready);

        // Reset the state of any child nodes.
        foreach (var child in Children)
        {
            child.Reset();
        }
    }

    /// <summary>
    /// Abort the running of this node.
    /// </summary>
    /// <param name="agent">The agent.</param>
    public override void Abort(IAgent agent)
    {
        // There is nothing to do if this node is not in the running state.
        if (!Is(State.Running))
        {
            return;
        }

        // Abort any child nodes.
        foreach (var child in Children)
        {
            child.Abort(agent);
        }

        // Reset the state of this node.
        Reset();

        Attributes.Exit?.CallAgentFunction(Options.Lookup, agent, false, true);
    }

    /// <summary>
    /// Gets the details of this node instance.
    /// </summary>
    /// <returns>The details of this node instance.</returns>
    public override NodeDetails GetDetails()
    {
        var details = base.GetDetails();
        // Manual conversion to avoid LINQ allocations
        var childrenDetails = new List<NodeDetails>(Children.Count);
        for (int i = 0; i < Children.Count; i++)
        {
            childrenDetails.Add(Children[i].GetDetails());
        }
        details.Children = childrenDetails;
        return details;
    }
}

