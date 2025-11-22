namespace Mistreevous;

/// <summary>
/// An ENTRY callback which defines an agent function to call when the associated node is updated and moves out of running state.
/// </summary>
public class Entry : Callback
{
    /// <summary>
    /// Creates a new instance of the Entry class.
    /// </summary>
    /// <param name="functionName">The name of the agent function to call.</param>
    /// <param name="args">The array of callback argument definitions.</param>
    public Entry(string functionName, object?[] args)
        : base("entry", args, functionName)
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
    /// <param name="agent">The agent.</param>
    /// <param name="isSuccess">Whether the node succeeded (not used for entry callbacks).</param>
    /// <param name="isAborted">Whether the node was aborted (not used for entry callbacks).</param>
    public override void CallAgentFunction(IAgent agent, bool isSuccess = false, bool isAborted = false)
    {
        // Attempt to get the invoker for the callback function.
        var callbackFuncInvoker = Lookup.GetFuncInvoker(agent, FunctionName);

        // The callback function should be defined.
        if (callbackFuncInvoker == null)
        {
            throw new Exception(
                $"cannot call entry function '{FunctionName}' as is not defined on the agent and has not been registered"
            );
        }

        // Call the callback function.
        callbackFuncInvoker(Args);
    }
}

