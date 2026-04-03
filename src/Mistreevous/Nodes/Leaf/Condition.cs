namespace Mistreevous;

/// <summary>
/// A Condition leaf node.
/// This represents a boolean condition that must be satisfied.
/// </summary>
public class Condition : Leaf
{
    private readonly string _conditionName;
    private readonly Arguments _conditionArguments;

    /// <summary>
    /// Creates a new instance of the Condition class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="conditionName">The condition name.</param>
    /// <param name="conditionArguments">The array of condition arguments.</param>
    public Condition(List<Attribute> attributes, BehaviourTreeOptions options, string conditionName, NodeArgument[]? conditionArguments)
        : base("condition", attributes, options)
    {
        _conditionName = conditionName;
        _conditionArguments = new Arguments(conditionArguments);
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // Attempt to get the invoker for the condition function.
        var conditionFuncInvoker = Options.Lookup.GetFuncInvoker(agent, _conditionName);

        // The condition function should be defined.
        if (conditionFuncInvoker == null)
        {
            throw new Exception(
                $"cannot update condition node as the condition '{_conditionName}' function is not defined on the agent and has not been registered"
            );
        }

        object? conditionFunctionResult;

        try
        {
            // Call the condition function, the result of which should be a boolean.
            conditionFunctionResult = conditionFuncInvoker(_conditionArguments.EvaluateArguments(agent));
        }
        catch (Exception error)
        {
            // An uncaught error was thrown.
            throw new Exception($"condition function '{_conditionName}' threw: {error.Message}", error);
        }

        // The result of calling the condition function must be a boolean value.
        if (conditionFunctionResult is not bool boolResult)
        {
            throw new Exception(
                $"expected condition function '{_conditionName}' to return a boolean but returned '{conditionFunctionResult}'"
            );
        }

        // Set the state of this node based on the condition result.
        SetState(boolResult ? State.Succeeded : State.Failed);
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => _conditionName;

    /// <summary>
    /// Gets the details of this node instance.
    /// </summary>
    /// <returns>The details of this node instance.</returns>
    public override NodeDetails GetDetails()
    {
        var details = base.GetDetails();
        details.Args = _conditionArguments;
        return details;
    }

    /// <summary>
    /// Called when the state of this node changes.
    /// </summary>
    /// <param name="previousState">The previous node state.</param>
    protected override void OnStateChanged(State previousState)
    {
        Options.OnNodeStateChange?.Invoke(new NodeStateChange
        {
            Id = GetUid(),
            Type = GetType(),
            Args = _conditionArguments,
            While = Attributes.While?.GetDetails() as GuardAttributeDetails,
            Until = Attributes.Until?.GetDetails() as GuardAttributeDetails,
            Entry = Attributes.Entry?.GetDetails() as CallbackAttributeDetails,
            Step = Attributes.Step?.GetDetails() as CallbackAttributeDetails,
            Exit = Attributes.Exit?.GetDetails() as CallbackAttributeDetails,
            PreviousState = previousState,
            State = GetState()
        });
    }
}

