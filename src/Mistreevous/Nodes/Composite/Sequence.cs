namespace Mistreevous;

/// <summary>
/// A SEQUENCE node.
/// The child nodes are executed in sequence until one fails or all succeed.
/// </summary>
public class Sequence : Composite
{
    /// <summary>
    /// Creates a new instance of the Sequence class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="children">The child nodes.</param>
    public Sequence(List<Attribute> attributes, BehaviourTreeOptions options, List<Node> children)
        : base("sequence", attributes, options, children)
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

            // If the current child has a state of 'SUCCEEDED' then we should move on to the next child.
            if (child.GetState() == State.Succeeded)
            {
                // Find out if the current child is the last one in the sequence.
                // If it is then this sequence node has also succeeded.
                if (i == Children.Count - 1)
                {
                    // This node is a 'SUCCEEDED' node.
                    SetState(State.Succeeded);

                    // There is no need to check the rest of the sequence as we have completed it.
                    return;
                }
                else
                {
                    // The child node succeeded, but we have not finished the sequence yet.
                    continue;
                }
            }

            // If the current child has a state of 'FAILED' then this node is also a 'FAILED' node.
            if (child.GetState() == State.Failed)
            {
                // This node is a 'FAILED' node.
                SetState(State.Failed);

                // There is no need to check the rest of the sequence.
                return;
            }

            // The node should be in the 'RUNNING' state.
            if (child.GetState() == State.Running)
            {
                // This node is a 'RUNNING' node.
                SetState(State.Running);

                // There is no need to check the rest of the sequence as the current child is still running.
                return;
            }

            // The child node was not in an expected state.
            throw new Exception("child node was not in an expected state.");
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "SEQUENCE";
}

