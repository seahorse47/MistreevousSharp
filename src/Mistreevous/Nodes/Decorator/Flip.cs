namespace Mistreevous;

/// <summary>
/// A Flip node.
/// This node wraps a single child and will flip the state of the child state.
/// </summary>
public class Flip : Decorator
{
    /// <summary>
    /// Creates a new instance of the Flip class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="child">The child node.</param>
    public Flip(List<Attribute> attributes, BehaviourTreeOptions options, Node child)
        : base("flip", attributes, options, child)
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
                SetState(State.Failed);
                break;

            case State.Failed:
                SetState(State.Succeeded);
                break;

            default:
                SetState(State.Ready);
                break;
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => "FLIP";
}

