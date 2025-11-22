namespace Mistreevous;

/// <summary>
/// A LOTTO node.
/// A winning child is picked on the initial update of this node, based on ticket weighting.
/// The state of this node will match the state of the winning child.
/// </summary>
public class Lotto : Composite
{
    private readonly double[]? _weights;

    /// <summary>
    /// Creates a new instance of the Lotto class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="weights">The child node weights.</param>
    /// <param name="children">The child nodes.</param>
    public Lotto(List<Attribute> attributes, BehaviourTreeOptions options, double[]? weights, List<Node> children)
        : base("lotto", attributes, options, children)
    {
        _weights = weights;
    }

    /// <summary>
    /// The child node selected to be the active one.
    /// </summary>
    private Node? _selectedChild = null;

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If this node is in the READY state then we need to pick a winning child node.
        if (Is(State.Ready))
        {
            // Randomly pick a child based on ticket weighting, this will become the active child for this composite node.
            _selectedChild = DrawLotto();
        }

        // If something went wrong and we don't have an active child then we should throw an error.
        if (_selectedChild == null)
        {
            throw new Exception("failed to update lotto node as it has no active child");
        }

        // If the selected child has never been updated or is running then we will need to update it now.
        if (_selectedChild.GetState() == State.Ready || _selectedChild.GetState() == State.Running)
        {
            _selectedChild.Update(agent);
        }

        // The state of the lotto node is the state of its selected child.
        SetState(_selectedChild.GetState());
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName()
    {
        if (_weights != null && _weights.Length > 0)
        {
            var weightStrings = new string[_weights.Length];
            for (int i = 0; i < _weights.Length; i++)
            {
                weightStrings[i] = _weights[i].ToString();
            }
            return $"LOTTO [{string.Join(",", weightStrings)}]";
        }
        else
        {
            return "LOTTO";
        }
    }

    /// <summary>
    /// Draws a winning child node based on ticket weighting.
    /// </summary>
    /// <returns>The selected child node.</returns>
    private Node DrawLotto()
    {
        if (Children.Count == 0)
        {
            return null!;
        }

        // Calculate total weight
        double totalWeight = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            double weight = (i < _weights?.Length) ? _weights[i] : 1.0;
            if (weight < 0)
            {
                weight = 0;
            }
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            // If no valid weights, pick first child
            return Children[0];
        }

        // Get random value
        var random = Options.Random ?? (() => new Random().NextDouble());
        double randomValue = random() * totalWeight;

        // Find the selected child based on weight
        double currentWeight = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            double weight = (i < _weights?.Length) ? _weights[i] : 1.0;
            if (weight < 0)
            {
                weight = 0;
            }
            currentWeight += weight;

            if (randomValue <= currentWeight)
            {
                return Children[i];
            }
        }

        // Fallback to last child (shouldn't happen, but safety)
        return Children[Children.Count - 1];
    }
}

