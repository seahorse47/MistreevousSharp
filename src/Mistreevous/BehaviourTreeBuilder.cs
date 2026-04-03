using Newtonsoft.Json.Linq;

namespace Mistreevous;

/// <summary>
/// Interface for querying named root node definitions.
/// </summary>
public interface IRootNodeDefinitionMap
{
    /// <summary>
    /// Attempt to get the definition that has the specified name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns>`true` if the definition exists</returns>
    bool TryGetDefinition(string name, out RootNodeDefinition? value);
}

/// <summary>
/// Builder for creating behaviour tree node instances from definitions.
/// </summary>
public static class BehaviourTreeBuilder
{
    private const string MainRootNodeKey = "__root__";

    /// <summary>
    /// Build and populate the root nodes based on the provided definition, assuming that the definition has been validated.
    /// </summary>
    /// <param name="definition">The root node definitions.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <returns>The built and populated root node definitions.</returns>
    public static Root BuildRootNode(IReadOnlyList<RootNodeDefinition> definition, BehaviourTreeOptions options)
    {
        // Create a mapping of root node identifiers to root node definitions, including globally registered subtree root node definitions.
        var (mainDefinition, rootNodeDefinitionMap) = FindMainDefinitionAndCreateDefinitionMap(definition);
        return BuildRootNode(mainDefinition, options, rootNodeDefinitionMap);
    }

    /// <summary>
    /// Build and populate the root nodes based on the provided definition, assuming that the definition has been validated.
    /// </summary>
    /// <param name="mainDefinition">The main root node definition.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="rootNodeDefinitionMap">For querying node definitions referenced by the provided main definition.</param>
    /// <returns>The built and populated root node definitions.</returns>
    public static Root BuildRootNode(RootNodeDefinition mainDefinition, BehaviourTreeOptions options, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        var rootNode = NodeFactory(mainDefinition, rootNodeDefinitionMap, options) as Root;

        if (rootNode == null)
        {
            throw new Exception("failed to build root node");
        }

        // Set a guard path on every leaf of the tree to evaluate as part of each update.
        ApplyLeafNodeGuardPaths(rootNode);

        return rootNode;
    }

    private static Node NodeFactory(NodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap, BehaviourTreeOptions options)
    {
        // Create the attributes for the node.
        var attributes = CreateNodeAttributes(definition);

        // Create the node instance based on the definition type.
        switch (definition.Type)
        {
            case "root":
                return CreateRootNode(attributes, options, (RootNodeDefinition)definition, rootNodeDefinitionMap);
            case "sequence":
                return CreateSequenceNode(attributes, options, (CompositeNodeDefinition)definition, rootNodeDefinitionMap);
            case "selector":
                return CreateSelectorNode(attributes, options, (CompositeNodeDefinition)definition, rootNodeDefinitionMap);
            case "parallel":
                return CreateParallelNode(attributes, options, (CompositeNodeDefinition)definition, rootNodeDefinitionMap);
            case "race":
                return CreateRaceNode(attributes, options, (CompositeNodeDefinition)definition, rootNodeDefinitionMap);
            case "all":
                return CreateAllNode(attributes, options, (CompositeNodeDefinition)definition, rootNodeDefinitionMap);
            case "lotto":
                return CreateLottoNode(attributes, options, (LottoNodeDefinition)definition, rootNodeDefinitionMap);
            case "action":
                return new Action(attributes, options, ((ActionNodeDefinition)definition).Call, ((ActionNodeDefinition)definition).Args);
            case "condition":
                return new Condition(attributes, options, ((ConditionNodeDefinition)definition).Call, ((ConditionNodeDefinition)definition).Args);
            case "wait":
                return CreateWaitNode(attributes, options, (WaitNodeDefinition)definition);
            case "repeat":
                return CreateRepeatNode(attributes, options, (RepeatNodeDefinition)definition, rootNodeDefinitionMap);
            case "retry":
                return CreateRetryNode(attributes, options, (RetryNodeDefinition)definition, rootNodeDefinitionMap);
            case "flip":
                return CreateFlipNode(attributes, options, (FlipNodeDefinition)definition, rootNodeDefinitionMap);
            case "succeed":
                return CreateSucceedNode(attributes, options, (SucceedNodeDefinition)definition, rootNodeDefinitionMap);
            case "fail":
                return CreateFailNode(attributes, options, (FailNodeDefinition)definition, rootNodeDefinitionMap);
            case "branch":
                return NodeFactory(ResolveReferencedNode(rootNodeDefinitionMap, ((BranchNodeDefinition)definition).Ref).Child!, rootNodeDefinitionMap, options);
            default:
                throw new Exception($"unknown node type: {definition.Type}");
        }
    }

    private static Sequence CreateSequenceNode(List<Attribute> attributes, BehaviourTreeOptions options, CompositeNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("a sequence node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }
        return new Sequence(attributes, options, children);
    }

    private static Root CreateRootNode(List<Attribute> attributes, BehaviourTreeOptions options, RootNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Child == null)
        {
            throw new Exception("a root node must have a single child node defined");
        }
        return new Root(attributes, options, NodeFactory(definition.Child, rootNodeDefinitionMap, options));
    }

    private static Selector CreateSelectorNode(List<Attribute> attributes, BehaviourTreeOptions options, CompositeNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("a selector node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }
        return new Selector(attributes, options, children);
    }

    private static Parallel CreateParallelNode(List<Attribute> attributes, BehaviourTreeOptions options, CompositeNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("a parallel node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }
        return new Parallel(attributes, options, children);
    }

    private static Race CreateRaceNode(List<Attribute> attributes, BehaviourTreeOptions options, CompositeNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("a race node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }
        return new Race(attributes, options, children);
    }

    private static All CreateAllNode(List<Attribute> attributes, BehaviourTreeOptions options, CompositeNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("an all node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }
        return new All(attributes, options, children);
    }

    private static Lotto CreateLottoNode(List<Attribute> attributes, BehaviourTreeOptions options, LottoNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Children == null || definition.Children.Count == 0)
        {
            throw new Exception("a lotto node must have at least a single child node defined");
        }
        
        var children = new List<Node>(definition.Children.Count);
        for (int i = 0; i < definition.Children.Count; i++)
        {
            children.Add(NodeFactory(definition.Children[i], rootNodeDefinitionMap, options));
        }

        // Validate that if weights are defined, they match the number of children
        if (definition.Weights != null && definition.Weights.Length > 0)
        {
            if (definition.Weights.Length != children.Count)
            {
                throw new Exception($"expected a number of weight arguments matching the number of child nodes for lotto node (expected {children.Count}, got {definition.Weights.Length})");
            }
        }

        return new Lotto(attributes, options, definition.Weights, children);
    }

    private static Wait CreateWaitNode(List<Attribute> attributes, BehaviourTreeOptions options, WaitNodeDefinition definition)
    {
        ParseDuration(definition.Duration, out int? duration, out int? durationMin, out int? durationMax);
        return new Wait(attributes, options, duration, durationMin, durationMax);
    }

    private static Repeat CreateRepeatNode(List<Attribute> attributes, BehaviourTreeOptions options, RepeatNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        ParseIterations(definition.Iterations, out int? iterations, out int? iterationsMin, out int? iterationsMax);
        
        // Validate iteration values
        if (iterations.HasValue && iterations.Value <= 0)
        {
            throw new Exception("a repeat node must have a positive number of iterations if defined");
        }
        if (iterationsMin.HasValue && iterationsMax.HasValue)
        {
            if (iterationsMin.Value <= 0 || iterationsMax.Value <= 0)
            {
                throw new Exception("a repeat node must have a positive minimum and maximum iteration count if defined");
            }
            if (iterationsMin.Value > iterationsMax.Value)
            {
                throw new Exception("a repeat node must not have a minimum iteration count that exceeds the maximum iteration count");
            }
        }
        
        return new Repeat(attributes, options, iterations, iterationsMin, iterationsMax, NodeFactory(definition.Child!, rootNodeDefinitionMap, options));
    }

    private static Retry CreateRetryNode(List<Attribute> attributes, BehaviourTreeOptions options, RetryNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        ParseAttempts(definition.Attempts, out int? attempts, out int? attemptsMin, out int? attemptsMax);
        
        // Validate attempt values
        if (attempts.HasValue && attempts.Value <= 0)
        {
            throw new Exception("a retry node must have a positive number of attempts if defined");
        }
        if (attemptsMin.HasValue && attemptsMax.HasValue)
        {
            if (attemptsMin.Value <= 0 || attemptsMax.Value <= 0)
            {
                throw new Exception("a retry node must have a positive minimum and maximum attempt count if defined");
            }
            if (attemptsMin.Value > attemptsMax.Value)
            {
                throw new Exception("a retry node must not have a minimum attempt count that exceeds the maximum attempt count");
            }
        }
        
        return new Retry(attributes, options, attempts, attemptsMin, attemptsMax, NodeFactory(definition.Child!, rootNodeDefinitionMap, options));
    }

    private static Flip CreateFlipNode(List<Attribute> attributes, BehaviourTreeOptions options, FlipNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Child == null)
        {
            throw new Exception("a flip node must have a single child node defined");
        }
        return new Flip(attributes, options, NodeFactory(definition.Child, rootNodeDefinitionMap, options));
    }

    private static Succeed CreateSucceedNode(List<Attribute> attributes, BehaviourTreeOptions options, SucceedNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Child == null)
        {
            throw new Exception("a succeed node must have a single child node defined");
        }
        return new Succeed(attributes, options, NodeFactory(definition.Child, rootNodeDefinitionMap, options));
    }

    private static Fail CreateFailNode(List<Attribute> attributes, BehaviourTreeOptions options, FailNodeDefinition definition, IRootNodeDefinitionMap rootNodeDefinitionMap)
    {
        if (definition.Child == null)
        {
            throw new Exception("a fail node must have a single child node defined");
        }
        return new Fail(attributes, options, NodeFactory(definition.Child, rootNodeDefinitionMap, options));
    }

    private static void ParseDuration(object? duration, out int? durationValue, out int? durationMin, out int? durationMax)
    {
        durationValue = null;
        durationMin = null;
        durationMax = null;

        if (duration == null)
        {
            return;
        }

        if (duration is JToken jsonToken)
        {
            if (jsonToken.Type == JTokenType.Integer || jsonToken.Type == JTokenType.Float)
            {
                durationValue = jsonToken.Value<int>();
            }
            else if (jsonToken.Type == JTokenType.Array)
            {
                var array = jsonToken as JArray;
                if (array != null && array.Count >= 2)
                {
                    durationMin = array[0].Value<int>();
                    durationMax = array[1].Value<int>();
                }
            }
        }
        else if (duration is int intValue)
        {
            durationValue = intValue;
        }
        else if (duration is double doubleValue)
        {
            durationValue = (int)doubleValue;
        }
        else if (duration is int[] intArray)
        {
            if (intArray.Length >= 2)
            {
                durationMin = intArray[0];
                durationMax = intArray[1];
            }
            else if (intArray.Length == 1)
            {
                durationValue = intArray[0];
            }
        }
    }

    private static void ParseIterations(object? iterations, out int? iterationsValue, out int? iterationsMin, out int? iterationsMax)
    {
        iterationsValue = null;
        iterationsMin = null;
        iterationsMax = null;

        if (iterations == null)
        {
            return;
        }

        if (iterations is JToken jsonToken)
        {
            if (jsonToken.Type == JTokenType.Integer || jsonToken.Type == JTokenType.Float)
            {
                iterationsValue = jsonToken.Value<int>();
            }
            else if (jsonToken.Type == JTokenType.Array)
            {
                var array = jsonToken as JArray;
                if (array != null && array.Count >= 2)
                {
                    iterationsMin = array[0].Value<int>();
                    iterationsMax = array[1].Value<int>();
                }
            }
        }
        else if (iterations is int intValue)
        {
            iterationsValue = intValue;
        }
        else if (iterations is double doubleValue)
        {
            iterationsValue = (int)doubleValue;
        }
        else if (iterations is int[] intArray)
        {
            if (intArray.Length >= 2)
            {
                iterationsMin = intArray[0];
                iterationsMax = intArray[1];
            }
            else if (intArray.Length == 1)
            {
                iterationsValue = intArray[0];
            }
        }
    }

    private static void ParseAttempts(object? attempts, out int? attemptsValue, out int? attemptsMin, out int? attemptsMax)
    {
        attemptsValue = null;
        attemptsMin = null;
        attemptsMax = null;

        if (attempts == null)
        {
            return;
        }

        if (attempts is JToken jsonToken)
        {
            if (jsonToken.Type == JTokenType.Integer || jsonToken.Type == JTokenType.Float)
            {
                attemptsValue = jsonToken.Value<int>();
            }
            else if (jsonToken.Type == JTokenType.Array)
            {
                var array = jsonToken as JArray;
                if (array != null && array.Count >= 2)
                {
                    attemptsMin = array[0].Value<int>();
                    attemptsMax = array[1].Value<int>();
                }
            }
        }
        else if (attempts is int intValue)
        {
            attemptsValue = intValue;
        }
        else if (attempts is double doubleValue)
        {
            attemptsValue = (int)doubleValue;
        }
        else if (attempts is int[] intArray)
        {
            if (intArray.Length >= 2)
            {
                attemptsMin = intArray[0];
                attemptsMax = intArray[1];
            }
            else if (intArray.Length == 1)
            {
                attemptsValue = intArray[0];
            }
        }
    }

    private static List<Attribute> CreateNodeAttributes(NodeDefinition definition)
    {
        var attributes = new List<Attribute>();

        if (definition.While != null)
        {
            attributes.Add(new While(definition.While));
        }

        if (definition.Until != null)
        {
            attributes.Add(new Until(definition.Until));
        }

        if (definition.Entry != null)
        {
            attributes.Add(new Entry(definition.Entry.Call, definition.Entry.Args));
        }

        if (definition.Step != null)
        {
            attributes.Add(new Step(definition.Step.Call, definition.Step.Args));
        }

        if (definition.Exit != null)
        {
            attributes.Add(new Exit(definition.Exit.Call, definition.Exit.Args));
        }

        return attributes;
    }

    private static (RootNodeDefinition, IRootNodeDefinitionMap) FindMainDefinitionAndCreateDefinitionMap(IReadOnlyList<RootNodeDefinition> definition)
    {
        var rootNodeMap = new SimpleRootNodeDefinitionMap();

        // Add in any registered subtree root node definitions.
        foreach (var (name, rootNodeDefinition) in Lookup.Default.GetSubtrees())
        {
            rootNodeMap[name] = new RootNodeDefinition
            {
                Type = rootNodeDefinition.Type,
                Id = name,
                Child = rootNodeDefinition.Child
            };
        }

        RootNodeDefinition mainDefinition = definition[0];
        // Populate the map with the root node definitions that were included with the tree definition.
        foreach (var rootNodeDefinition in definition)
        {
            if (string.IsNullOrEmpty(rootNodeDefinition.Id))
            {
                mainDefinition = rootNodeDefinition;
            }
            else
            {
                rootNodeMap[rootNodeDefinition.Id] = rootNodeDefinition;
            }
        }

        return (mainDefinition, rootNodeMap);
    }

    private static RootNodeDefinition ResolveReferencedNode(IRootNodeDefinitionMap rootNodeDefinitionMap, string key)
    {
        if (!rootNodeDefinitionMap.TryGetDefinition(key, out var referencedNodeDefinition))
        {
            throw new Exception($"referenced node not found for key: {key}");
        }

        return referencedNodeDefinition!;
    }

    private static void ApplyLeafNodeGuardPaths(Root root)
    {
        var nodePaths = new List<List<Node>>();

        void FindLeafNodes(List<Node> path, Node node)
        {
            path = new List<Node>(path) { node };

            if (node is Leaf)
            {
                nodePaths.Add(path);
            }
            else if (node is Composite composite)
            {
                foreach (var child in composite.GetChildren())
                {
                    FindLeafNodes(path, child);
                }
            }
            else if (node is Decorator decorator)
            {
                FindLeafNodes(path, decorator.GetChildren()[0]);
            }
        }

        FindLeafNodes(new List<Node>(), root);

        foreach (var path in nodePaths)
        {
            for (int depth = 0; depth < path.Count; depth++)
            {
                var currentNode = path[depth];

                if (currentNode.HasGuardPath())
                {
                    continue;
                }

                var guardPathParts = new List<GuardPathPart>();
                for (int i = 0; i <= depth; i++)
                {
                    var pathNode = path[i];
                    var guards = new List<Guard>();
                    var attrs = pathNode.GetAttributes();
                    for (int j = 0; j < attrs.Count; j++)
                    {
                        if (attrs[j] is Guard guard)
                        {
                            guards.Add(guard);
                        }
                    }
                    if (guards.Count > 0)
                    {
                        guardPathParts.Add(new GuardPathPart
                        {
                            Node = pathNode,
                            Guards = guards
                        });
                    }
                }

                if (guardPathParts.Count > 0)
                {
                    currentNode.SetGuardPath(new GuardPath(guardPathParts));
                }
            }
        }
    }

    private class SimpleRootNodeDefinitionMap : Dictionary<string, RootNodeDefinition>, IRootNodeDefinitionMap
    {
        public bool TryGetDefinition(string name, out RootNodeDefinition? value)
        {
            return TryGetValue(name, out value);
        }
    }
}
