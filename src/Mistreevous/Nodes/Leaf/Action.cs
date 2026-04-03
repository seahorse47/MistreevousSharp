namespace Mistreevous;

/// <summary>
/// An Action leaf node.
/// This represents an immediate or ongoing state of behaviour.
/// </summary>
public class Action : Leaf
{
    private readonly string _actionName;
    private readonly Arguments _actionArguments;
    private bool _isUsingUpdatePromise = false;
    private Task<State>? _updatePromise;
    private State? _updatePromiseResult;

    /// <summary>
    /// Creates a new instance of the Action class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="actionName">The action name.</param>
    /// <param name="actionArguments">The array of action arguments.</param>
    public Action(List<Attribute> attributes, BehaviourTreeOptions options, string actionName, NodeArgument[]? actionArguments)
        : base("action", attributes, options)
    {
        _actionName = actionName;
        _actionArguments = new Arguments(actionArguments);
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If the result of this action depends on an update promise then there is nothing to do until it settles.
        if (_isUsingUpdatePromise)
        {
            // Are we still waiting for our update promise to settle?
            if (_updatePromise != null && !_updatePromise.IsCompleted)
            {
                return;
            }

            if (_updatePromiseResult.HasValue)
            {
                var result = _updatePromiseResult.Value;

                // Our update promise settled, check to make sure the result is a valid finished state.
                if (result != State.Succeeded && result != State.Failed)
                {
                    throw new Exception(
                        "action node promise resolved with an invalid value, expected a State.Succeeded or State.Failed value to be returned"
                    );
                }

                // Set the state of this node to match the state returned by the promise.
                SetState(result);
                _updatePromiseResult = null;
                _isUsingUpdatePromise = false;
                _updatePromise = null;
                return;
            }
        }

        // Attempt to get the invoker for the action function.
        var actionFuncInvoker = Lookup.GetFuncInvoker(agent, _actionName);

        // The action function should be defined.
        if (actionFuncInvoker == null)
        {
            throw new Exception(
                $"cannot update action node as the action '{_actionName}' function is not defined on the agent and has not been registered"
            );
        }

        object? actionFunctionResult;

        try
        {
            // Call the action function, the result of which may be:
            // - The finished state of this action node.
            // - A Task to return a finished node state.
            // - Null if the node should remain in the running state.
            actionFunctionResult = actionFuncInvoker(_actionArguments.EvaluateArguments(agent));
        }
        catch (Exception error)
        {
            // An uncaught error was thrown.
            throw new Exception($"action function '{_actionName}' threw: {error.Message}", error);
        }

        if (actionFunctionResult is Task<State> task)
        {
            _updatePromise = task;
            _isUsingUpdatePromise = true;

            // This node will be in the 'RUNNING' state until the update promise resolves.
            SetState(State.Running);

            // Continue the task to handle the result on the next update.
            task.ContinueWith(t =>
            {
                if (_isUsingUpdatePromise && t.IsCompletedSuccessfully)
                {
                    _updatePromiseResult = t.Result;
                }
                else if (_isUsingUpdatePromise && t.IsFaulted)
                {
                    throw new Exception($"action function '{_actionName}' promise rejected with '{t.Exception?.GetBaseException().Message}'");
                }
            });
        }
        else
        {
            // Validate the returned value.
            ValidateUpdateResult(actionFunctionResult);

            // Set the state of this node, this may be null, which just means that the node is still in the 'RUNNING' state.
            SetState(actionFunctionResult as State? ?? State.Running);
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName() => _actionName;

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public override void Reset()
    {
        // Reset the state of this node.
        SetState(State.Ready);

        // There is no longer an update promise that we care about.
        _isUsingUpdatePromise = false;
        _updatePromiseResult = null;
        _updatePromise = null;
    }

    /// <summary>
    /// Gets the details of this node instance.
    /// </summary>
    /// <returns>The details of this node instance.</returns>
    public override NodeDetails GetDetails()
    {
        var details = base.GetDetails();
        details.Args = _actionArguments;
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
            Args = _actionArguments,
            While = Attributes.While?.GetDetails() as GuardAttributeDetails,
            Until = Attributes.Until?.GetDetails() as GuardAttributeDetails,
            Entry = Attributes.Entry?.GetDetails() as CallbackAttributeDetails,
            Step = Attributes.Step?.GetDetails() as CallbackAttributeDetails,
            Exit = Attributes.Exit?.GetDetails() as CallbackAttributeDetails,
            PreviousState = previousState,
            State = GetState()
        });
    }

    /// <summary>
    /// Validate the result of an update function call.
    /// </summary>
    /// <param name="result">The result of an update function call.</param>
    private void ValidateUpdateResult(object? result)
    {
        switch (result)
        {
            case State.Succeeded:
            case State.Failed:
            case State.Running:
            case null:
                return;
            default:
                throw new Exception(
                    $"expected action function '{_actionName}' to return an optional State.Succeeded or State.Failed value but returned '{result}'"
                );
        }
    }
}

