namespace Mistreevous;

/// <summary>
/// A decorator node that wraps a single child node.
/// </summary>
public abstract class Decorator : Node
{
    /// <summary>
    /// The child node.
    /// </summary>
    protected readonly Node Child;

    /// <summary>
    /// Creates a new instance of the Decorator class.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="child">The child node.</param>
    protected Decorator(string type, List<Attribute> attributes, BehaviourTreeOptions options, Node child)
        : base(type, attributes, options)
    {
        Child = child;
    }

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public List<Node> GetChildren() => new List<Node> { Child };

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public override void Reset()
    {
        // Reset the state of this node.
        SetState(State.Ready);

        // Reset the state of the child node.
        Child.Reset();
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

        // Abort the child node.
        Child.Abort(agent);

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
        details.Children = new List<NodeDetails> { Child.GetDetails() };
        return details;
    }
}

