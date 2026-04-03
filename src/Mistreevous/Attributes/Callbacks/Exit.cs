namespace Mistreevous;

/// <summary>
/// An EXIT callback which defines an agent function to call when the associated node finishes processing.
/// </summary>
public class Exit : Callback
{
    private readonly object?[] _allArgs;

    /// <summary>
    /// Creates a new instance of the Exit class.
    /// </summary>
    /// <param name="functionName">The name of the agent function to call.</param>
    /// <param name="args">The array of callback argument definitions.</param>
    public Exit(string functionName, NodeArgument[]? args)
        : base("exit", args, functionName)
    {
        _allArgs = new object?[Args.Length + 1];
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
    /// <param name="isSuccess">Whether the node succeeded.</param>
    /// <param name="isAborted">Whether the node was aborted.</param>
    public override void CallAgentFunction(ILookup lookup, IAgent agent, bool isSuccess = false, bool isAborted = false)
    {
        // Attempt to get the invoker for the callback function.
        var callbackFuncInvoker = lookup.GetFuncInvoker(agent, FunctionName);

        // The callback function should be defined.
        if (callbackFuncInvoker == null)
        {
            throw new Exception(
                $"cannot call exit function '{FunctionName}' as is not defined on the agent and has not been registered"
            );
        }

        // Create the exit function argument object.
        var exitArg = new ExitArg(isSuccess, isAborted);
        _allArgs[0] = exitArg;
        if (_allArgs.Length > 1)
        {
            Array.Copy(Args.EvaluateArguments(agent), 0, _allArgs, 1, _allArgs.Length);
        }

        // Call the callback function with the exit argument.
        callbackFuncInvoker(_allArgs);
    }

    /// <summary>
    /// The type of first argument of exit callback.
    /// </summary>
    /// <param name="Succeeded">Whether the node succeeded.</param>
    /// <param name="Aborted">Whether the node was aborted.</param>
    public record ExitArg(bool Succeeded, bool Aborted);
}

