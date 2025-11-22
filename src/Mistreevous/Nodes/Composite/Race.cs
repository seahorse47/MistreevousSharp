namespace Mistreevous;

/// <summary>
/// A RACE node.
/// The child nodes are executed concurrently until one succeeds or all fail.
/// </summary>
public class Race : Composite
{
    /// <summary>
    /// Creates a new instance of the Race class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    public Race(List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base("race", attributes, options, children)
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

        // If any of our child nodes have succeeded then this node has also succeeded.
        bool hasSucceeded = false;
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].Is(State.Succeeded))
            {
                hasSucceeded = true;
                break;
            }
        }

        if (hasSucceeded)
        {
            // This node is a 'SUCCEEDED' node.
            SetState(State.Succeeded);

            // Abort every running child.
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.GetState() == State.Running)
                {
                    child.Abort(agent);
                }
            }

            return;
        }

        // A race node will move into the failed state if all child nodes move into the failed state as none can succeed.
        bool allFailed = true;
        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].Is(State.Failed))
            {
                allFailed = false;
                break;
            }
        }

        if (allFailed)
        {
            // This node is a 'FAILED' node.
            SetState(State.Failed);

            return;
        }

        // If we didn't move to a succeeded or failed state then this node is still running.
        SetState(State.Running);
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "RACE";
}

