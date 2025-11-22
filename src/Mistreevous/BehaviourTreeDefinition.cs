using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mistreevous;

/// <summary>
/// Represents a node argument that can be a value or an agent property reference.
/// </summary>
[JsonConverter(typeof(NodeArgumentConverter))]
public class NodeArgument
{
    /// <summary>
    /// Gets or sets the value of the argument.
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the agent property name if this argument references an agent property.
    /// </summary>
    public string? AgentProperty { get; set; }

    /// <summary>
    /// Gets whether this argument is an agent property reference.
    /// </summary>
    public bool IsAgentProperty => AgentProperty != null;
}

/// <summary>
/// An attribute for a node.
/// </summary>
public class NodeAttributeDefinition
{
    /// <summary>
    /// The name of the agent function or globally registered function to invoke.
    /// </summary>
    public string Call { get; set; } = string.Empty;

    /// <summary>
    /// An array of arguments to pass when invoking the agent function.
    /// </summary>
    public NodeArgument[]? Args { get; set; }
}

/// <summary>
/// A guard attribute for a node.
/// </summary>
public class NodeGuardDefinition : NodeAttributeDefinition
{
    /// <summary>
    /// The flag defining whether the attributed node would move to the SUCCEEDED state when aborted by this guard, otherwise FAILED.
    /// </summary>
    public bool? SucceedOnAbort { get; set; }
}

/// <summary>
/// A type defining a general node definition.
/// </summary>
public class NodeDefinition
{
    /// <summary>
    /// The node type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The 'while' node attribute definition.
    /// </summary>
    public NodeGuardDefinition? While { get; set; }

    /// <summary>
    /// The 'until' node attribute definition.
    /// </summary>
    public NodeGuardDefinition? Until { get; set; }

    /// <summary>
    /// The 'entry' node attribute definition.
    /// </summary>
    public NodeAttributeDefinition? Entry { get; set; }

    /// <summary>
    /// The 'exit' node attribute definition.
    /// </summary>
    public NodeAttributeDefinition? Exit { get; set; }

    /// <summary>
    /// The 'step' node attribute definition.
    /// </summary>
    public NodeAttributeDefinition? Step { get; set; }
}

/// <summary>
/// A composite node that can contain any number of child nodes.
/// </summary>
public class CompositeNodeDefinition : NodeDefinition
{
    /// <summary>
    /// The child nodes of this composite node.
    /// </summary>
    public List<NodeDefinition> Children { get; set; } = new();
}

/// <summary>
/// A decorator node, a composite with only a single child node.
/// </summary>
public class DecoratorNodeDefinition : NodeDefinition
{
    /// <summary>
    /// The child node of this decorator node.
    /// </summary>
    public NodeDefinition? Child { get; set; }
}

/// <summary>
/// A branch node.
/// </summary>
public class BranchNodeDefinition : NodeDefinition
{
    /// <summary>
    /// Creates a new instance of the BranchNodeDefinition class.
    /// </summary>
    public BranchNodeDefinition()
    {
        Type = "branch";
    }

    /// <summary>
    /// The reference matching a root node identifier.
    /// </summary>
    public string Ref { get; set; } = string.Empty;
}

/// <summary>
/// An action node.
/// </summary>
public class ActionNodeDefinition : NodeDefinition
{
    /// <summary>
    /// Creates a new instance of the ActionNodeDefinition class.
    /// </summary>
    public ActionNodeDefinition()
    {
        Type = "action";
    }

    /// <summary>
    /// The name of the agent function or globally registered function to invoke.
    /// </summary>
    public string Call { get; set; } = string.Empty;

    /// <summary>
    /// An array of arguments to pass when invoking the action function.
    /// </summary>
    public NodeArgument[]? Args { get; set; }
}

/// <summary>
/// A condition node.
/// </summary>
public class ConditionNodeDefinition : NodeDefinition
{
    /// <summary>
    /// Creates a new instance of the ConditionNodeDefinition class.
    /// </summary>
    public ConditionNodeDefinition()
    {
        Type = "condition";
    }

    /// <summary>
    /// The name of the agent function or globally registered function to invoke.
    /// </summary>
    public string Call { get; set; } = string.Empty;

    /// <summary>
    /// An array of arguments to pass when invoking the condition function.
    /// </summary>
    public NodeArgument[]? Args { get; set; }
}

/// <summary>
/// A wait node.
/// </summary>
public class WaitNodeDefinition : NodeDefinition
{
    /// <summary>
    /// Creates a new instance of the WaitNodeDefinition class.
    /// </summary>
    public WaitNodeDefinition()
    {
        Type = "wait";
    }

    /// <summary>
    /// The duration to wait in milliseconds if defined as a single integer, or the lower and upper duration bounds if defined as an array containing two integer values.
    /// </summary>
    public object? Duration { get; set; }
}

/// <summary>
/// A sequence node.
/// </summary>
public class SequenceNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the SequenceNodeDefinition class.
    /// </summary>
    public SequenceNodeDefinition()
    {
        Type = "sequence";
    }
}

/// <summary>
/// A selector node.
/// </summary>
public class SelectorNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the SelectorNodeDefinition class.
    /// </summary>
    public SelectorNodeDefinition()
    {
        Type = "selector";
    }
}

/// <summary>
/// A lotto node.
/// </summary>
public class LottoNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the LottoNodeDefinition class.
    /// </summary>
    public LottoNodeDefinition()
    {
        Type = "lotto";
    }

    /// <summary>
    /// The selection weights for child nodes that correspond to the child node position.
    /// </summary>
    public double[]? Weights { get; set; }
}

/// <summary>
/// A parallel node.
/// </summary>
public class ParallelNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the ParallelNodeDefinition class.
    /// </summary>
    public ParallelNodeDefinition()
    {
        Type = "parallel";
    }
}

/// <summary>
/// A race node.
/// </summary>
public class RaceNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the RaceNodeDefinition class.
    /// </summary>
    public RaceNodeDefinition()
    {
        Type = "race";
    }
}

/// <summary>
/// An all node.
/// </summary>
public class AllNodeDefinition : CompositeNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the AllNodeDefinition class.
    /// </summary>
    public AllNodeDefinition()
    {
        Type = "all";
    }
}

/// <summary>
/// A root node.
/// </summary>
public class RootNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the RootNodeDefinition class.
    /// </summary>
    public RootNodeDefinition()
    {
        Type = "root";
    }

    /// <summary>
    /// The unique root node identifier.
    /// </summary>
    public string? Id { get; set; }
}

/// <summary>
/// A repeat node.
/// </summary>
public class RepeatNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the RepeatNodeDefinition class.
    /// </summary>
    public RepeatNodeDefinition()
    {
        Type = "repeat";
    }

    /// <summary>
    /// The number of iterations to make if defined as a single number, or the lower and upper iteration bounds if defined as an array containing two integer values.
    /// </summary>
    public object? Iterations { get; set; }
}

/// <summary>
/// A retry node.
/// </summary>
public class RetryNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the RetryNodeDefinition class.
    /// </summary>
    public RetryNodeDefinition()
    {
        Type = "retry";
    }

    /// <summary>
    /// The number of attempts to make if defined as a single number, or the lower and upper attempt bounds if defined as an array containing two integer values.
    /// </summary>
    public object? Attempts { get; set; }
}

/// <summary>
/// A flip node.
/// </summary>
public class FlipNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the FlipNodeDefinition class.
    /// </summary>
    public FlipNodeDefinition()
    {
        Type = "flip";
    }
}

/// <summary>
/// A succeed node.
/// </summary>
public class SucceedNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the SucceedNodeDefinition class.
    /// </summary>
    public SucceedNodeDefinition()
    {
        Type = "succeed";
    }
}

/// <summary>
/// A fail node.
/// </summary>
public class FailNodeDefinition : DecoratorNodeDefinition
{
    /// <summary>
    /// Creates a new instance of the FailNodeDefinition class.
    /// </summary>
    public FailNodeDefinition()
    {
        Type = "fail";
    }
}

/// <summary>
/// JSON converter for NodeArgument to handle both direct values and agent property references.
/// </summary>
public class NodeArgumentConverter : JsonConverter<NodeArgument>
{
    /// <summary>
    /// Reads a NodeArgument from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The type of object to read.</param>
    /// <param name="existingValue">The existing value.</param>
    /// <param name="hasExistingValue">Whether an existing value exists.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>The deserialized NodeArgument.</returns>
    public override NodeArgument? ReadJson(JsonReader reader, Type objectType, NodeArgument? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var token = JToken.Load(reader);
        
        if (token.Type == JTokenType.Object)
        {
            var obj = token as JObject;
            if (obj != null && obj.TryGetValue("$", out var prop))
            {
                return new NodeArgument { AgentProperty = prop.Value<string>() };
            }
        }
        
        return new NodeArgument { Value = token.ToObject<object>() };
    }

    /// <summary>
    /// Writes a NodeArgument to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The NodeArgument to write.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(JsonWriter writer, NodeArgument? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value.IsAgentProperty)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$");
            writer.WriteValue(value.AgentProperty);
            writer.WriteEndObject();
        }
        else
        {
            if (value.Value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var token = JToken.FromObject(value.Value, serializer);
                token.WriteTo(writer);
            }
        }
    }
}

