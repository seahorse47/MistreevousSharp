namespace Mistreevous;

/// <summary>
/// A PARALLEL node.
/// The child nodes are executed concurrently until one fails or all succeed.
/// </summary>
public class Parallel : Composite
{
    /// <summary>
    /// Creates a new instance of the Parallel class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    public Parallel(List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base("parallel", attributes, options, children)
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

        // If any of our child nodes have failed then this node has also failed.
        bool hasFailed = false;
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].Is(State.Failed))
            {
                hasFailed = true;
                break;
            }
        }

        if (hasFailed)
        {
            // This node is a 'FAILED' node.
            SetState(State.Failed);

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

        // A parallel node will move into the succeeded state if all child nodes move into the succeeded state.
        bool allSucceeded = true;
        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].Is(State.Succeeded))
            {
                allSucceeded = false;
                break;
            }
        }

        if (allSucceeded)
        {
            // This node is a 'SUCCEEDED' node.
            SetState(State.Succeeded);

            return;
        }

        // If we didn't move to a succeeded or failed state then this node is still running.
        SetState(State.Running);
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "PARALLEL";
}

