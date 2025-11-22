namespace Mistreevous;

/// <summary>
/// A base node guard attribute.
/// </summary>
public abstract class Guard : Attribute
{
    /// <summary>
    /// The node guard definition.
    /// </summary>
    protected NodeGuardDefinition Definition { get; }

    /// <summary>
    /// Creates a new instance of the Guard class.
    /// </summary>
    /// <param name="type">The node attribute type.</param>
    /// <param name="definition">The node guard definition.</param>
    protected Guard(string type, NodeGuardDefinition definition)
        : base(type, ConvertGuardArguments(definition.Args))
    {
        Definition = definition;
    }

    private static object?[] ConvertGuardArguments(NodeArgument[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return Array.Empty<object?>();
        }

        // Manual conversion to avoid LINQ allocations
        var result = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            result[i] = args[i].IsAgentProperty ? (object?)args[i].AgentProperty : args[i].Value;
        }
        return result;
    }

    /// <summary>
    /// Gets the name of the condition function that determines whether the guard is satisfied.
    /// </summary>
    public string Condition => Definition.Call;

    /// <summary>
    /// Gets a flag defining whether the running node should move to the succeeded state when aborted, otherwise failed.
    /// </summary>
    public bool SucceedOnAbort => Definition.SucceedOnAbort ?? false;

    /// <summary>
    /// Gets the attribute details.
    /// </summary>
    public override object GetDetails()
    {
        return new GuardAttributeDetails
        {
            Call = Condition,
            Args = Args,
            SucceedOnAbort = SucceedOnAbort
        };
    }

    /// <summary>
    /// Gets whether the guard is satisfied.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <returns>Whether the guard is satisfied.</returns>
    public abstract bool IsSatisfied(IAgent agent);
}

