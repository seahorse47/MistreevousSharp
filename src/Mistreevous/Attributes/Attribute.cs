namespace Mistreevous;

/// <summary>
/// A base node attribute.
/// </summary>
public abstract class Attribute
{
    /// <summary>
    /// The node attribute type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The array of attribute arguments.
    /// </summary>
    public object?[] Args { get; }

    /// <summary>
    /// Creates a new instance of the Attribute class.
    /// </summary>
    /// <param name="type">The node attribute type.</param>
    /// <param name="args">The array of attribute arguments.</param>
    protected Attribute(string type, object?[] args)
    {
        Type = type;
        Args = args;
    }

    /// <summary>
    /// Gets the attribute details.
    /// </summary>
    public abstract object GetDetails();
}

