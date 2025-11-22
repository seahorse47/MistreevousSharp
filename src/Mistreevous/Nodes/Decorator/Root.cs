namespace Mistreevous;

/// <summary>
/// A Root node.
/// The root node will have a single child.
/// </summary>
public class Root : Decorator
{
    /// <summary>
    /// Creates a new instance of the Root class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="child">The child node.</param>
    public Root(List<Attribute> attributes, BehaviourTreeOptions options, Node child)
        : base("root", attributes, options, child)
    {
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If the child has never been updated or is running then we will need to update it now.
        if (Child.GetState() == State.Ready || Child.GetState() == State.Running)
        {
            // Update the child of this node.
            Child.Update(agent);
        }

        // The state of the root node is the state of its child.
        SetState(Child.GetState());
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "ROOT";
}

