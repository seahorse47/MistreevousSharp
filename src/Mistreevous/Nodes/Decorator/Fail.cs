namespace Mistreevous;

/// <summary>
/// A Fail node.
/// This node wraps a single child and will always move to the 'FAILED' state when the child moves to a 'SUCCEEDED' or 'FAILED' state.
/// </summary>
public class Fail : Decorator
{
    /// <summary>
    /// Creates a new instance of the Fail class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="child">The child node.</param>
    public Fail(List<Attribute> attributes, BehaviourTreeOptions options, Node child)
        : base("fail", attributes, options, child)
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
            Child.Update(agent);
        }

        // The state of this node will depend on the state of its child.
        switch (Child.GetState())
        {
            case State.Running:
                SetState(State.Running);
                break;

            case State.Succeeded:
            case State.Failed:
                SetState(State.Failed);
                break;

            default:
                SetState(State.Ready);
                break;
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "FAIL";
}

