namespace Mistreevous;

/// <summary>
/// A base node.
/// </summary>
public abstract class Node
{
    /// <summary>
    /// The node unique identifier.
    /// </summary>
    protected readonly string Uid;

    /// <summary>
    /// The node attributes.
    /// </summary>
    protected readonly NodeAttributes Attributes;

    /// <summary>
    /// The node state.
    /// </summary>
    private State _state = State.Ready;

    /// <summary>
    /// The guard path to evaluate as part of a node update.
    /// </summary>
    private GuardPath? _guardPath;

    /// <summary>
    /// The node type.
    /// </summary>
    private readonly string _type;

    /// <summary>
    /// The behaviour tree options.
    /// </summary>
    protected readonly BehaviourTreeOptions Options;

    /// <summary>
    /// Creates a new instance of the Node class.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    protected Node(string type, List<Attribute> attributes, BehaviourTreeOptions options)
    {
        _type = type;
        Options = options;
        Uid = Utilities.CreateUid();

        // Create our attribute mapping without LINQ allocations.
        Entry? entry = null;
        Step? step = null;
        Exit? exit = null;
        While? whileGuard = null;
        Until? until = null;

        for (int i = 0; i < attributes.Count; i++)
        {
            var attr = attributes[i];
            switch (attr)
            {
                case Entry e: entry = e; break;
                case Step s: step = s; break;
                case Exit ex: exit = ex; break;
                case While w: whileGuard = w; break;
                case Until u: until = u; break;
            }
        }

        Attributes = new NodeAttributes
        {
            Entry = entry,
            Step = step,
            Exit = exit,
            While = whileGuard,
            Until = until
        };
    }

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected abstract void OnUpdate(IAgent agent);

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public abstract string GetName();

    /// <summary>
    /// Gets the state of the node.
    /// </summary>
    public State GetState() => _state;

    /// <summary>
    /// Sets the state of the node.
    /// </summary>
    /// <param name="value">The new state.</param>
    protected void SetState(State value)
    {
        // Grab the original state of this node.
        var previousState = _state;

        // Set the new node state.
        _state = value;

        // If the state actually changed we should handle it.
        if (previousState != value)
        {
            OnStateChanged(previousState);
        }
    }

    /// <summary>
    /// Gets the unique id of the node.
    /// </summary>
    public string GetUid() => Uid;

    /// <summary>
    /// Gets the type of the node.
    /// </summary>
    public new string GetType() => _type;

    // Reusable list for GetAttributes to avoid allocations
    private static readonly ThreadLocal<List<Attribute>> _attributesList = new(() => new List<Attribute>(5));

    /// <summary>
    /// Gets the node attributes.
    /// Zero-allocation version that reuses a thread-local list.
    /// </summary>
    public List<Attribute> GetAttributes()
    {
        var list = _attributesList.Value!;
        list.Clear();
        if (Attributes.Entry != null) list.Add(Attributes.Entry);
        if (Attributes.Step != null) list.Add(Attributes.Step);
        if (Attributes.Exit != null) list.Add(Attributes.Exit);
        if (Attributes.While != null) list.Add(Attributes.While);
        if (Attributes.Until != null) list.Add(Attributes.Until);
        return list;
    }

    /// <summary>
    /// Sets the guard path to evaluate as part of a node update.
    /// </summary>
    public void SetGuardPath(GuardPath value) => _guardPath = value;

    /// <summary>
    /// Gets whether a guard path is assigned to this node.
    /// </summary>
    public bool HasGuardPath() => _guardPath != null;

    /// <summary>
    /// Gets whether this node is in the specified state.
    /// </summary>
    /// <param name="value">The value to compare to the node state.</param>
    public bool Is(State value) => _state == value;

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public virtual void Reset()
    {
        SetState(State.Ready);
    }

    /// <summary>
    /// Abort the running of this node.
    /// </summary>
    /// <param name="agent">The agent.</param>
    public virtual void Abort(IAgent agent)
    {
        // There is nothing to do if this node is not in the running state.
        if (!Is(State.Running))
        {
            return;
        }

        // Reset the state of this node.
        Reset();

        Attributes.Exit?.CallAgentFunction(agent, false, true);
    }

    /// <summary>
    /// Update the node.
    /// </summary>
    /// <param name="agent">The agent.</param>
    public void Update(IAgent agent)
    {
        // If this node is already in a 'SUCCEEDED' or 'FAILED' state then there is nothing to do.
        if (Is(State.Succeeded) || Is(State.Failed))
        {
            return;
        }

        try
        {
            // Evaluate all of the guard path conditions for the current tree path.
            _guardPath?.Evaluate(agent);

            // If this node is in the READY state then call the ENTRY for this node if it exists.
            if (Is(State.Ready))
            {
                Attributes.Entry?.CallAgentFunction(agent);
            }

            Attributes.Step?.CallAgentFunction(agent);

            // Do the actual update.
            OnUpdate(agent);

            // If this node is now in a 'SUCCEEDED' or 'FAILED' state then call the EXIT for this node if it exists.
            if (Is(State.Succeeded) || Is(State.Failed))
            {
                Attributes.Exit?.CallAgentFunction(agent, Is(State.Succeeded), false);
            }
        }
        catch (GuardUnsatisfiedException error)
        {
            // If the error is a GuardUnsatisfiedException then we need to determine if this node is the source.
            if (error.IsSourceNode(this))
            {
                // Abort the current node.
                Abort(agent);

                // Any node that is the source of an abort will move to a resolved state.
                SetState(error.Guard.SucceedOnAbort ? State.Succeeded : State.Failed);
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Gets the details of this node instance.
    /// </summary>
    /// <returns>The details of this node instance.</returns>
    public virtual NodeDetails GetDetails()
    {
        return new NodeDetails
        {
            Id = Uid,
            Name = GetName(),
            Type = _type,
            While = Attributes.While?.GetDetails() as GuardAttributeDetails,
            Until = Attributes.Until?.GetDetails() as GuardAttributeDetails,
            Entry = Attributes.Entry?.GetDetails() as CallbackAttributeDetails,
            Step = Attributes.Step?.GetDetails() as CallbackAttributeDetails,
            Exit = Attributes.Exit?.GetDetails() as CallbackAttributeDetails,
            State = _state
        };
    }

    /// <summary>
    /// Called when the state of this node changes.
    /// </summary>
    /// <param name="previousState">The previous node state.</param>
    protected virtual void OnStateChanged(State previousState)
    {
        // We should call the onNodeStateChange callback if it was defined.
        Options.OnNodeStateChange?.Invoke(new NodeStateChange
        {
            Id = Uid,
            Type = _type,
            While = Attributes.While?.GetDetails() as GuardAttributeDetails,
            Until = Attributes.Until?.GetDetails() as GuardAttributeDetails,
            Entry = Attributes.Entry?.GetDetails() as CallbackAttributeDetails,
            Step = Attributes.Step?.GetDetails() as CallbackAttributeDetails,
            Exit = Attributes.Exit?.GetDetails() as CallbackAttributeDetails,
            PreviousState = previousState,
            State = _state
        });
    }
}

/// <summary>
/// A mapping of attribute names to attributes configured for a node.
/// </summary>
public class NodeAttributes
{
    /// <summary>
    /// Gets or sets the entry callback attribute.
    /// </summary>
    public Entry? Entry { get; set; }
    
    /// <summary>
    /// Gets or sets the step callback attribute.
    /// </summary>
    public Step? Step { get; set; }
    
    /// <summary>
    /// Gets or sets the exit callback attribute.
    /// </summary>
    public Exit? Exit { get; set; }
    
    /// <summary>
    /// Gets or sets the while guard attribute.
    /// </summary>
    public While? While { get; set; }
    
    /// <summary>
    /// Gets or sets the until guard attribute.
    /// </summary>
    public Until? Until { get; set; }
}

