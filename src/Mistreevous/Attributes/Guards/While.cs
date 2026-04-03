namespace Mistreevous;

/// <summary>
/// A WHILE guard which is satisfied as long as the given condition remains true.
/// </summary>
public class While : Guard
{
    /// <summary>
    /// Creates a new instance of the While class.
    /// </summary>
    /// <param name="definition">The while node guard definition.</param>
    public While(NodeGuardDefinition definition)
        : base("while", definition)
    {
    }

    /// <summary>
    /// Gets whether the guard is satisfied.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <returns>Whether the guard is satisfied.</returns>
    public override bool IsSatisfied(IAgent agent)
    {
        // Attempt to get the invoker for the condition function.
        var conditionFuncInvoker = Lookup.GetFuncInvoker(agent, Condition);

        // The condition function should be defined.
        if (conditionFuncInvoker == null)
        {
            throw new Exception(
                $"cannot evaluate node guard as the condition '{Condition}' function is not defined on the agent and has not been registered"
            );
        }

        object? conditionFunctionResult;

        try
        {
            // Call the guard condition function to determine the state of this node, the result of which should be a boolean.
            conditionFunctionResult = conditionFuncInvoker(Args.EvaluateArguments(agent));
        }
        catch (Exception error)
        {
            // An uncaught error was thrown.
            throw new Exception($"guard condition function '{Condition}' threw: {error.Message}", error);
        }

        // The result of calling the guard condition function must be a boolean value.
        if (conditionFunctionResult is not bool boolResult)
        {
            throw new Exception(
                $"expected guard condition function '{Condition}' to return a boolean but returned '{conditionFunctionResult}'"
            );
        }

        // Return whether this guard is satisfied.
        return boolResult;
    }
}

