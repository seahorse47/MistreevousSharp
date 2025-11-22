namespace Mistreevous;

/// <summary>
/// A SELECTOR node.
/// The child nodes are executed in sequence until one succeeds or all fail.
/// </summary>
public class Selector : Composite
{
    /// <summary>
    /// Creates a new instance of the Selector class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    public Selector(List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base("selector", attributes, options, children)
    {
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // Iterate over all of the children of this node.
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];

            // If the child has never been updated or is running then we will need to update it now.
            if (child.GetState() == State.Ready || child.GetState() == State.Running)
            {
                // Update the child of this node.
                child.Update(agent);
            }

            // If the current child has a state of 'SUCCEEDED' then this node is also a 'SUCCEEDED' node.
            if (child.GetState() == State.Succeeded)
            {
                // This node is a 'SUCCEEDED' node.
                SetState(State.Succeeded);
                return;
            }

            // If the current child has a state of 'FAILED' then we should move on to the next child.
            if (child.GetState() == State.Failed)
            {
                // Find out if the current child is the last one in the selector.
                // If it is then this selector node has also failed.
                if (i == Children.Count - 1)
                {
                    // This node is a 'FAILED' node.
                    SetState(State.Failed);
                    return;
                }
                else
                {
                    // The child node failed, but we have not finished the selector yet.
                    continue;
                }
            }

            // The node should be in the 'RUNNING' state.
            if (child.GetState() == State.Running)
            {
                // This node is a 'RUNNING' node.
                SetState(State.Running);
                return;
            }

            // The child node was not in an expected state.
            throw new Exception("child node was not in an expected state.");
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "SELECTOR";
}

