namespace Mistreevous;

/// <summary>
/// An UNTIL guard which is satisfied as long as the given condition remains false.
/// </summary>
public class Until : Guard
{
    /// <summary>
    /// Creates a new instance of the Until class.
    /// </summary>
    /// <param name="definition">The until node guard definition.</param>
    public Until(NodeGuardDefinition definition)
        : base("until", definition)
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
            conditionFunctionResult = conditionFuncInvoker(Args);
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

        // Return whether this guard is satisfied (inverted for UNTIL).
        return !boolResult;
    }
}

