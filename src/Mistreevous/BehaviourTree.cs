using Newtonsoft.Json;
using Mistreevous.Parsing;

namespace Mistreevous;

/// <summary>
/// A representation of a behaviour tree.
/// </summary>
public class BehaviourTree
{
    /// <summary>
    /// The main root tree node.
    /// </summary>
    private readonly Root _rootNode;

    /// <summary>
    /// The agent instance that this behaviour tree is modelling behaviour for.
    /// </summary>
    private readonly IAgent _agent;

    /// <summary>
    /// The behaviour tree options.
    /// </summary>
    private readonly BehaviourTreeOptions _options;

    /// <summary>
    /// Creates a new instance of the BehaviourTree class.
    /// </summary>
    /// <param name="definition">The behaviour tree definition as an MDSL string.</param>
    /// <param name="agent">The agent instance that this behaviour tree is modelling behaviour for.</param>
    /// <param name="options">The behaviour tree options object.</param>
    public BehaviourTree(string definition, IAgent agent, BehaviourTreeOptions? options = null)
    {
        // The tree definition must be defined.
        if (string.IsNullOrEmpty(definition))
        {
            throw new Exception("tree definition not defined");
        }

        // The agent must be defined and not null.
        if (agent == null)
        {
            throw new Exception("the agent must be an object and not null");
        }

        _agent = agent;
        _options = options ?? new BehaviourTreeOptions();

        // We should validate the definition before we try to build the tree nodes.
        var validationResult = ValidateMDSLDefinition(definition);

        ThrowIfValidationFailed(validationResult);

        try
        {
            // Create the populated tree of behaviour tree nodes and get the root node.
            _rootNode = BehaviourTreeBuilder.BuildRootNode(validationResult.Json!, _options);
        }
        catch (Exception exception)
        {
            // There was an issue in trying build and populate the behaviour tree.
            throw new Exception($"error building tree: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Creates a new instance of the BehaviourTree class.
    /// </summary>
    /// <param name="definition">The behaviour tree definition object.</param>
    /// <param name="agent">The agent instance that this behaviour tree is modelling behaviour for.</param>
    /// <param name="options">The behaviour tree options object.</param>
    /// <param name="skipValidation">Whether skip validation of the definition object.</param>
    public BehaviourTree(RootNodeDefinition definition, IAgent agent, BehaviourTreeOptions? options = null, bool skipValidation = false)
        : this(definition != null ? new List<RootNodeDefinition> { definition } : throw new Exception("tree definition not defined"), agent, options, skipValidation)
    {
    }

    /// <summary>
    /// Creates a new instance of the BehaviourTree class.
    /// </summary>
    /// <param name="definition">The behaviour tree definition as a list of root node definition objects.</param>
    /// <param name="agent">The agent instance that this behaviour tree is modelling behaviour for.</param>
    /// <param name="options">The behaviour tree options object.</param>
    /// <param name="skipValidation">Whether skip validation of the definition objects.</param>
    public BehaviourTree(List<RootNodeDefinition> definition, IAgent agent, BehaviourTreeOptions? options = null, bool skipValidation = false)
    {
        // The tree definition must be defined.
        if (definition == null)
        {
            throw new Exception("tree definition not defined");
        }

        // The agent must be defined and not null.
        if (agent == null)
        {
            throw new Exception("the agent must be an object and not null");
        }

        _agent = agent;
        _options = options ?? new BehaviourTreeOptions();

        if (!skipValidation)
        {
            // We should validate the definition before we try to build the tree nodes.
            var validationResult = ValidateJsonDefinitions(definition);

            ThrowIfValidationFailed(validationResult);
        }

        try
        {
            // Create the populated tree of behaviour tree nodes and get the root node.
            _rootNode = BehaviourTreeBuilder.BuildRootNode(definition, _options);
        }
        catch (Exception exception)
        {
            // There was an issue in trying build and populate the behaviour tree.
            throw new Exception($"error building tree: {exception.Message}", exception);
        }
    }

    private static void ThrowIfValidationFailed(DefinitionValidationResult validationResult)
    {
        // Did our validation fail without error?
        if (!validationResult.Succeeded)
        {
            throw new Exception($"invalid definition: {validationResult.ErrorMessage}");
        }

        // Double check that we did actually get our json definition as part of our definition validation.
        if (validationResult.Json == null)
        {
            throw new Exception(
                "expected json definition to be returned as part of successful definition validation response"
            );
        }
    }

    /// <summary>
    /// Gets whether the tree is in the RUNNING state.
    /// </summary>
    /// <returns>true if the tree is in the RUNNING state, otherwise false.</returns>
    public bool IsRunning()
    {
        return _rootNode.GetState() == State.Running;
    }

    /// <summary>
    /// Gets the current tree state of SUCCEEDED, FAILED, READY or RUNNING.
    /// </summary>
    /// <returns>The current tree state.</returns>
    public State GetState()
    {
        return _rootNode.GetState();
    }

    /// <summary>
    /// Step the tree.
    /// Carries out a node update that traverses the tree from the root node outwards to any child nodes, skipping those that are already in a resolved state of SUCCEEDED or FAILED.
    /// After being updated, leaf nodes will have a state of SUCCEEDED, FAILED or RUNNING. Leaf nodes that are left in the RUNNING state as part of a tree step will be revisited each
    /// subsequent step until they move into a resolved state of either SUCCEEDED or FAILED, after which execution will move through the tree to the next node with a state of READY.
    ///
    /// Calling this method when the tree is already in a resolved state of SUCCEEDED or FAILED will cause it to be reset before tree traversal begins.
    /// </summary>
    public void Step()
    {
        // If the root node has already been stepped to completion then we need to reset it.
        if (_rootNode.GetState() == State.Succeeded || _rootNode.GetState() == State.Failed)
        {
            _rootNode.Reset();
        }

        try
        {
            _rootNode.Update(_agent);
        }
        catch (Exception exception)
        {
            throw new Exception($"error stepping tree: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Resets the tree from the root node outwards to each nested node, giving each a state of READY.
    /// </summary>
    public void Reset()
    {
        _rootNode.Reset();
    }

    /// <summary>
    /// Gets the details of every node in the tree, starting from the root.
    /// </summary>
    /// <returns>The details of every node in the tree, starting from the root.</returns>
    public NodeDetails GetTreeNodeDetails()
    {
        return _rootNode.GetDetails();
    }

    /// <summary>
    /// Registers the action/condition/guard/callback function or subtree with the given name.
    /// </summary>
    /// <param name="name">The name of the function or subtree to register.</param>
    /// <param name="value">The function or subtree definition to register.</param>
    public static void Register(string name, object value)
    {
        // Are we going to register a action/condition/guard/callback function?
        if (value is GlobalFunction globalFunction)
        {
            Lookup.SetFunc(name, globalFunction);
            return;
        }

        // We are not registering an action/condition/guard/callback function, so we must be registering a subtree.
        if (value is string mdslDefinition)
        {
            // We will assume that any string passed in will be a mdsl definition.
            List<RootNodeDefinition> rootNodeDefinitions;
            try
            {
                rootNodeDefinitions = MDSLDefinitionParser.ConvertMDSLToJSON(mdslDefinition);
            }
            catch (Exception exception)
            {
                throw new Exception($"error registering definition, invalid MDSL: {exception.Message}", exception);
            }

            // This function should only ever be called with a definition containing a single unnamed root node.
            if (rootNodeDefinitions.Count != 1 || !string.IsNullOrEmpty(rootNodeDefinitions[0].Id))
            {
                throw new Exception("error registering definition: expected a single unnamed root node");
            }

            try
            {
                // We should validate the subtree as we don't want invalid subtrees available via the lookup.
                var validationResult = ValidateJSONDefinitionInternal(rootNodeDefinitions[0]);

                // Did our validation fail without error?
                if (!validationResult.Succeeded)
                {
                    throw new Exception(validationResult.ErrorMessage);
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"error registering definition: {exception.Message}", exception);
            }

            // Everything seems hunky-dory, register the subtree.
            Lookup.SetSubtree(name, rootNodeDefinitions[0]);
        }
        else if (value is RootNodeDefinition rootNodeDefinition)
        {
            // We will assume that any object passed in is a root node definition.

            try
            {
                // We should validate the subtree as we don't want invalid subtrees available via the lookup.
                var validationResult = ValidateJSONDefinitionInternal(rootNodeDefinition);

                // Did our validation fail without error?
                if (!validationResult.Succeeded)
                {
                    throw new Exception(validationResult.ErrorMessage);
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"error registering definition: {exception.Message}", exception);
            }

            // Everything seems hunky-dory, register the subtree.
            Lookup.SetSubtree(name, rootNodeDefinition);
        }
        else
        {
            throw new Exception("unexpected value, expected string mdsl definition, root node json definition or function");
        }
    }

    /// <summary>
    /// Unregisters the registered action/condition/guard/callback function or subtree with the given name.
    /// </summary>
    /// <param name="name">The name of the registered action/condition/guard/callback function or subtree to unregister.</param>
    public static void Unregister(string name)
    {
        Lookup.Remove(name);
    }

    /// <summary>
    /// Unregister all registered action/condition/guard/callback functions and subtrees.
    /// </summary>
    public static void UnregisterAll()
    {
        Lookup.Empty();
    }

    /// <summary>
    /// Validates a behaviour tree definition.
    /// </summary>
    /// <param name="definition">The definition to validate.</param>
    /// <returns>A validation result.</returns>
    public static DefinitionValidationResult ValidateDefinition(object definition)
    {
        if (definition == null)
        {
            return DefinitionValidationResult.CreateFailure("definition is null");
        }

        if (definition is string mdslDefinition)
        {
            return ValidateMDSLDefinition(mdslDefinition);
        }

        if (definition is RootNodeDefinition rootNodeDefinition)
        {
            return ValidateJSONDefinition(rootNodeDefinition);
        }

        if (definition is List<RootNodeDefinition> rootNodeDefinitions)
        {
            return ValidateJsonDefinitions(rootNodeDefinitions);
        }

        // Try to deserialize as JSON.
        try
        {
            var jsonString = definition.ToString();
            if (string.IsNullOrEmpty(jsonString))
            {
                return DefinitionValidationResult.CreateFailure("definition is empty");
            }

            var jsonDefinition = JsonConvert.DeserializeObject<RootNodeDefinition>(jsonString);
            if (jsonDefinition != null)
            {
                return ValidateJSONDefinition(jsonDefinition);
            }
        }
        catch
        {
            // Not valid JSON, continue
        }

        return DefinitionValidationResult.CreateFailure($"unexpected definition type of '{definition.GetType().Name}'");
    }

    /// <summary>
    /// Convert MDSL into behaviour tree definitions and validate it.
    /// </summary>
    /// <param name="definition">The MDSL source.</param>
    /// <returns>A validation result.</returns>
    public static DefinitionValidationResult ValidateMDSLDefinition(string definition)
    {
        if (string.IsNullOrEmpty(definition))
        {
            return DefinitionValidationResult.CreateFailure("definition is null or empty");
        }

        List<RootNodeDefinition> rootNodeDefinitions;
        try
        {
            rootNodeDefinitions = MDSLDefinitionParser.ConvertMDSLToJSON(definition);
        }
        catch (Exception exception)
        {
            return DefinitionValidationResult.CreateFailure(exception.Message);
        }

        // Unpack all of the root node definitions into arrays of main ('id' defined) and sub ('id' not defined) root node definitions.
        var mainRootNodeDefinitions = new List<RootNodeDefinition>();
        var subRootNodeDefinitions = new List<RootNodeDefinition>();
        for (int i = 0; i < rootNodeDefinitions.Count; i++)
        {
            var root = rootNodeDefinitions[i];
            if (string.IsNullOrEmpty(root.Id))
            {
                mainRootNodeDefinitions.Add(root);
            }
            else
            {
                subRootNodeDefinitions.Add(root);
            }
        }

        // We should ALWAYS have exactly one root node definition without an 'id' property defined, which is out main root node definition.
        if (mainRootNodeDefinitions.Count != 1)
        {
            return DefinitionValidationResult.CreateFailure(
                "expected single unnamed root node at base of definition to act as main root"
            );
        }

        // We should never have duplicate 'id' properties across our sub root node definitions.
        var subRootNodeIdentifiers = new HashSet<string>();
        foreach (var rootNodeDefinition in subRootNodeDefinitions)
        {
            if (!string.IsNullOrEmpty(rootNodeDefinition.Id))
            {
                if (subRootNodeIdentifiers.Contains(rootNodeDefinition.Id))
                {
                    return DefinitionValidationResult.CreateFailure(
                        $"duplicate root node identifier '{rootNodeDefinition.Id}' found in definition"
                    );
                }
                subRootNodeIdentifiers.Add(rootNodeDefinition.Id);
            }
        }

        // Validate each root node definition.
        foreach (var rootNodeDefinition in rootNodeDefinitions)
        {
            var result = ValidateJSONDefinitionInternal(rootNodeDefinition);
            if (!result.Succeeded)
            {
                return DefinitionValidationResult.CreateFailure(result.ErrorMessage ?? "validation failed");
            }
        }

        return new DefinitionValidationResult
        {
            Succeeded = true,
            ErrorMessage = null,
            Json = rootNodeDefinitions
        };
    }

    /// <summary>
    /// Validates a list of behaviour tree definitions.
    /// </summary>
    /// <param name="rootNodeDefinitions">The definition to validate.</param>
    /// <returns>A validation result.</returns>
    public static DefinitionValidationResult ValidateJsonDefinitions(List<RootNodeDefinition> rootNodeDefinitions)
    {
        // Validate each root node definition.
        foreach (var nodeDefinition in rootNodeDefinitions)
        {
            var result = ValidateJSONDefinitionInternal(nodeDefinition);
            if (!result.Succeeded)
            {
                return new DefinitionValidationResult
                {
                    Succeeded = false,
                    ErrorMessage = result.ErrorMessage,
                    Json = null
                };
            }
        }

        return new DefinitionValidationResult
        {
            Succeeded = true,
            ErrorMessage = null,
            Json = rootNodeDefinitions
        };
    }

    /// <summary>
    /// Validates a behaviour tree definition.
    /// </summary>
    /// <param name="definition">The definition to validate.</param>
    /// <returns>A validation result.</returns>
    public static DefinitionValidationResult ValidateJSONDefinition(RootNodeDefinition definition)
    {
        var result = ValidateJSONDefinitionInternal(definition);
        if (result.Succeeded)
        {
            result.Json = new List<RootNodeDefinition> { definition };
        }

        return result;
    }

    private static DefinitionValidationResult ValidateJSONDefinitionInternal(RootNodeDefinition definition)
    {
        // Basic validation - check that the definition has a type and required properties
        if (string.IsNullOrEmpty(definition.Type))
        {
            return DefinitionValidationResult.CreateFailure("node definition must have a type");
        }

        // Validate the node structure recursively
        try
        {
            ValidateNode(definition, 0);
        }
        catch (Exception exception)
        {
            return DefinitionValidationResult.CreateFailure(exception.Message);
        }

        return new DefinitionValidationResult
        {
            Succeeded = true,
            ErrorMessage = null,
        };
    }

    private static void ValidateNode(NodeDefinition definition, int depth)
    {
        if (depth > 1000)
        {
            throw new Exception("maximum tree depth exceeded");
        }

        // Validate based on node type
        if (definition is DecoratorNodeDefinition decoratorNode)
        {
            if (decoratorNode.Child == null)
            {
                throw new Exception($"a {definition.Type} node must have a single child node defined");
            }
            ValidateNode(decoratorNode.Child, depth + 1);
        }
        else if (definition is CompositeNodeDefinition compositeNode)
        {
            if (compositeNode.Children == null || compositeNode.Children.Count == 0)
            {
                throw new Exception($"a {definition.Type} node must have at least a single child node defined");
            }
            foreach (var child in compositeNode.Children)
            {
                ValidateNode(child, depth + 1);
            }
        }
    }
}

/// <summary>
/// Result of definition validation.
/// </summary>
public class DefinitionValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the validated JSON definition.
    /// </summary>
    public List<RootNodeDefinition>? Json { get; set; }

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failure validation result.</returns>
    public static DefinitionValidationResult CreateFailure(string errorMessage)
    {
        return new DefinitionValidationResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage,
            Json = null
        };
    }
}

