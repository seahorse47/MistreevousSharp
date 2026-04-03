namespace Mistreevous;

/// <summary>
/// A STEP callback which defines an agent function to call each time the associated node is updated.
/// </summary>
public class Step : Callback
{
    /// <summary>
    /// Creates a new instance of the Step class.
    /// </summary>
    /// <param name="functionName">The name of the agent function to call.</param>
    /// <param name="args">The array of callback argument definitions.</param>
    public Step(string functionName, NodeArgument[]? args)
        : base("step", args, functionName)
    {
    }

    /// <summary>
    /// Gets the attribute details.
    /// </summary>
    public override object GetDetails()
    {
        return new CallbackAttributeDetails
        {
            Call = FunctionName,
            Args = Args
        };
    }

    /// <summary>
    /// Attempt to call the agent function that this callback refers to.
    /// </summary>
    /// <param name="lookup">The lookup instance.</param>
    /// <param name="agent">The agent.</param>
    /// <param name="isSuccess">Whether the node succeeded (not used for step callbacks).</param>
    /// <param name="isAborted">Whether the node was aborted (not used for step callbacks).</param>
    public override void CallAgentFunction(ILookup lookup, IAgent agent, bool isSuccess = false, bool isAborted = false)
    {
        // Attempt to get the invoker for the callback function.
        var callbackFuncInvoker = lookup.GetFuncInvoker(agent, FunctionName);

        // The callback function should be defined.
        if (callbackFuncInvoker == null)
        {
            throw new Exception(
                $"cannot call step function '{FunctionName}' as is not defined on the agent and has not been registered"
            );
        }

        // Call the callback function.
        callbackFuncInvoker(Args.EvaluateArguments(agent));
    }
}

