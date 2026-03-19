namespace Mistreevous;

/// <summary>
/// An EXIT callback which defines an agent function to call when the associated node finishes processing.
/// </summary>
public class Exit : Callback
{
    /// <summary>
    /// Creates a new instance of the Exit class.
    /// </summary>
    /// <param name="functionName">The name of the agent function to call.</param>
    /// <param name="args">The array of callback argument definitions.</param>
    public Exit(string functionName, object?[] args)
        : base("exit", args, functionName)
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
    /// <param name="isSuccess">Whether the node succeeded.</param>
    /// <param name="isAborted">Whether the node was aborted.</param>
    public override void CallAgentFunction(IAgent agent, bool isSuccess = false, bool isAborted = false)
    {
        // Attempt to get the invoker for the callback function.
        var callbackFuncInvoker = Lookup.GetFuncInvoker(agent, FunctionName);

        // The callback function should be defined.
        if (callbackFuncInvoker == null)
        {
            throw new Exception(
                $"cannot call exit function '{FunctionName}' as is not defined on the agent and has not been registered"
            );
        }

        // Create the exit function argument object.
        var exitArg = new ExitArg(isSuccess, isAborted);

        // Call the callback function with the exit argument.
        // Manual array concatenation to avoid LINQ allocations
        var allArgs = new object?[Args.Length + 1];
        allArgs[0] = exitArg;
        for (int i = 0; i < Args.Length; i++)
        {
            allArgs[i + 1] = Args[i];
        }
        callbackFuncInvoker(allArgs);
    }

    public record ExitArg(bool Succeeded, bool Aborted);
}

