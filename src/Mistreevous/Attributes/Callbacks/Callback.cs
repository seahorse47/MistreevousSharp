namespace Mistreevous;

/// <summary>
/// A base node callback attribute.
/// </summary>
public abstract class Callback : Attribute
{
    /// <summary>
    /// The name of the agent function to call.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Creates a new instance of the Callback class.
    /// </summary>
    /// <param name="type">The node attribute type.</param>
    /// <param name="args">The array of decorator argument definitions.</param>
    /// <param name="functionName">The name of the agent function to call.</param>
    protected Callback(string type, NodeArgument[]? args, string functionName)
        : base(type, args)
    {
        FunctionName = functionName;
    }

    /// <summary>
    /// Attempt to call the agent function that this callback refers to.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="isSuccess">Whether the node succeeded.</param>
    /// <param name="isAborted">Whether the node was aborted.</param>
    public abstract void CallAgentFunction(IAgent agent, bool isSuccess = false, bool isAborted = false);
}

