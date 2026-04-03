namespace Mistreevous;

/// <summary>
/// Holds an array of node arguments. It's also responsible for evaluating argument values for a given agent.
/// </summary>
public class Arguments
{
    /// <summary>
    /// The array of node arguments.
    /// </summary>
    public NodeArgument[] NodeArguments { get; }

    /// <summary>
    /// Number of arguments.
    /// </summary>
    public int Length { get => NodeArguments.Length; }

    private readonly object?[] _convertedArgs;
    private readonly Evaluator? _evaluators;

    /// <summary>
    /// Creates a new instance of the Arguments class.
    /// </summary>
    /// <param name="nodeArguments">Node arguments.</param>
    public Arguments(NodeArgument[]? nodeArguments)
    {
        this.NodeArguments = nodeArguments ?? Array.Empty<NodeArgument>();
        this._convertedArgs = ConvertArguments(NodeArguments, out _evaluators);
    }

    /// <summary>
    /// Evaluate argument values for the given agent.
    /// </summary>
    /// <param name="agent">The agent used for the evaluation.</param>
    /// <returns>The evaluated argument values.</returns>
    public object?[] EvaluateArguments(IAgent agent)
    {
        var evaluator = this._evaluators;
        while (evaluator != null)
        {
            _convertedArgs[evaluator.Index] = agent[evaluator.PropertyName];
            evaluator = evaluator.Next;
        }

        return _convertedArgs;
    }

    private static object?[] ConvertArguments(NodeArgument[] nodeArguments, out Evaluator? evaluators)
    {
        evaluators = null;
        Evaluator? lastEvaluator = null;

        var convertedArgs = new object?[nodeArguments.Length];
        for (int i = 0; i < nodeArguments.Length; i++)
        {
            var arg = nodeArguments[i];
            if (arg.IsAgentProperty)
            {
                convertedArgs[i] = null;
                var evaluator = new Evaluator(i, arg.AgentProperty!);
                if (lastEvaluator == null)
                {
                    evaluators = lastEvaluator = evaluator;
                }
                else
                {
                    lastEvaluator.Next = evaluator;
                }
            }
            else
            {
                convertedArgs[i] = arg.Value;
            }
        }

        return convertedArgs;
    }

    private class Evaluator
    {
        public int Index { get; }
        public string PropertyName { get; }

        public Evaluator? Next { get; set; }

        public Evaluator(int index, string propertyName)
        {
            Index = index;
            PropertyName = propertyName;
            Next = null;
        }
    }
}
