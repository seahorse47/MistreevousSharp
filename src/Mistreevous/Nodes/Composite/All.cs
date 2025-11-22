namespace Mistreevous;

/// <summary>
/// An ALL node.
/// The child nodes are executed concurrently until all child nodes move to a completed state.
/// </summary>
public class All : Composite
{
    /// <summary>
    /// Creates a new instance of the All class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    public All(List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base("all", attributes, options, children)
    {
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // Iterate over all of the children of this node, updating any that aren't in a settled state.
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            // If the child has never been updated or is running then we will need to update it now.
            if (child.GetState() == State.Ready || child.GetState() == State.Running)
            {
                // Update the child of this node.
                child.Update(agent);
            }
        }

        // An all node will move into a completed state if all child nodes move into a completed state.
        bool allCompleted = true;
        for (int i = 0; i < Children.Count; i++)
        {
            var state = Children[i].GetState();
            if (state != State.Succeeded && state != State.Failed)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            // If any of our child nodes have succeeded then this node has also succeeded, otherwise it has failed.
            bool hasSucceeded = false;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Is(State.Succeeded))
                {
                    hasSucceeded = true;
                    break;
                }
            }

            SetState(hasSucceeded ? State.Succeeded : State.Failed);

            return;
        }

        // If we didn't move to a succeeded or failed state then this node is still running.
        SetState(State.Running);
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "ALL";
}

