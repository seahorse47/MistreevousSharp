namespace Mistreevous;

/// <summary>
/// A leaf node.
/// </summary>
public abstract class Leaf : Node
{
    /// <summary>
    /// Creates a new instance of the Leaf class.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    protected Leaf(string type, List<Attribute> attributes, BehaviourTreeOptions options)
        : base(type, attributes, options)
    {
    }
}

