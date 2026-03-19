using System.Text.RegularExpressions;

namespace Mistreevous.Parsing;

/// <summary>
/// Parser for MDSL (Mistreevous Domain Specific Language) definitions.
/// </summary>
public static class MDSLDefinitionParser
{
    /// <summary>
    /// Convert the MDSL tree definition string into an equivalent JSON definition.
    /// </summary>
    /// <param name="definition">The tree definition string as MDSL.</param>
    /// <returns>The root node JSON definitions.</returns>
    public static List<RootNodeDefinition> ConvertMDSLToJSON(string definition)
    {
        // Parse our definition string into a bunch of tokens.
        var tokeniseResult = Tokenise(definition);

        // Convert the tokens that we parsed from the MDSL definition into JSON and return it.
        return ConvertTokensToJSONDefinition(tokeniseResult.Tokens, tokeniseResult.Placeholders);
    }

    private static readonly string SingleCharTokens = "(){}[],";
    private static readonly string[] SingleCharTokenStrings = { "(", ")", "{", "}", "[", "]", "," };

    private static TokeniseResult Tokenise(string definition)
    {
        // Clean the definition by removing any comments.
        definition = Regex.Replace(definition, @"/\*(.|\n)*?\*/", "");

        // Swap out any node/attribute argument string literals with a placeholder
        var stringLiteralResult = SubstituteStringLiterals(definition);
        var processedDefinition = stringLiteralResult.ProcessedDefinition;

        // Manual tokenization to avoid LINQ allocations
        var tokens = new List<string>();
        if (processedDefinition.Length > 0)
        {
            int start = 0;
            for (int i = 0; i < processedDefinition.Length; i++)
            {
                var c = processedDefinition[i];
                var isWhiteSpace = char.IsWhiteSpace(c);
                var singleCharTokenIndex = !isWhiteSpace ? SingleCharTokens.IndexOf(c) : -1;
                if (isWhiteSpace || singleCharTokenIndex >= 0)
                {
                    if (start >= 0 && i > start)
                    {
                        tokens.Add(processedDefinition.Substring(start, i - start));
                        start = -1;
                    }

                    if (singleCharTokenIndex >= 0)
                    {
                        tokens.Add(SingleCharTokenStrings[singleCharTokenIndex]);
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = i;
                }
            }

            if (start >= 0 && start < processedDefinition.Length)
            {
                tokens.Add(processedDefinition.Substring(start));
            }
        }

        return new TokeniseResult
        {
            Tokens = tokens,
            Placeholders = stringLiteralResult.Placeholders
        };
    }

    private static StringLiteralSubstitutionResult SubstituteStringLiterals(string definition)
    {
        var placeholders = new Dictionary<string, string>();

        // Manual replacement to avoid LINQ allocations
        var processedDefinition = Regex.Replace(definition, @"""(\\.|[^""\\])*""", match =>
        {
            var strippedMatch = match.Value.Substring(1, match.Value.Length - 2);
            
            // Manual search to avoid LINQ
            string? existingPlaceholder = null;
            foreach (var kvp in placeholders)
            {
                if (kvp.Value == strippedMatch)
                {
                    existingPlaceholder = kvp.Key;
                    break;
                }
            }

            if (existingPlaceholder == null)
            {
                var placeholder = $"@@{placeholders.Count}@@";
                placeholders[placeholder] = strippedMatch;
                return placeholder;
            }

            return existingPlaceholder;
        });

        return new StringLiteralSubstitutionResult
        {
            Placeholders = placeholders,
            ProcessedDefinition = processedDefinition
        };
    }

    private static List<RootNodeDefinition> ConvertTokensToJSONDefinition(List<string> tokens, Dictionary<string, string> placeholders)
    {
        if (tokens.Count < 3)
        {
            throw new Exception("invalid token count");
        }

        // We should have a matching number of '{' and '}' tokens.
        // Manual count to avoid LINQ allocations
        int openBraces = 0, closeBraces = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "{") openBraces++;
            else if (tokens[i] == "}") closeBraces++;
        }
        if (openBraces != closeBraces)
        {
            throw new Exception("scope character mismatch");
        }

        var treeStacks = new List<List<NodeDefinition>>();
        var rootNodes = new List<RootNodeDefinition>();

        // Helper function to push a node onto the tree stack
        void PushNode(NodeDefinition node)
        {
            if (node is RootNodeDefinition rootNode)
            {
                if (treeStacks.Count > 0 && treeStacks[^1].Count > 0)
                {
                    throw new Exception("a root node cannot be the child of another node");
                }
                rootNodes.Add(rootNode);
                treeStacks.Add(new List<NodeDefinition> { rootNode });
                return;
            }

            if (treeStacks.Count == 0 || treeStacks[^1].Count == 0)
            {
                throw new Exception("expected root node at base of definition");
            }

            var topTreeStack = treeStacks[^1];
            var topNode = topTreeStack[^1];

            if (topNode is CompositeNodeDefinition compositeNode)
            {
                if (compositeNode.Children == null)
                {
                    compositeNode.Children = new List<NodeDefinition>();
                }
                compositeNode.Children.Add(node);
            }
            else if (topNode is DecoratorNodeDefinition decoratorNode)
            {
                if (decoratorNode.Child != null)
                {
                    throw new Exception("a decorator node must only have a single child node");
                }
                decoratorNode.Child = node;
            }

            if (!(node is ActionNodeDefinition || node is ConditionNodeDefinition || node is WaitNodeDefinition || node is BranchNodeDefinition))
            {
                topTreeStack.Add(node);
            }
        }

        // Helper function to pop a node from the tree stack
        NodeDefinition? PopNode()
        {
            if (treeStacks.Count == 0)
            {
                return null;
            }

            var topTreeStack = treeStacks[^1];
            if (topTreeStack.Count == 0)
            {
                return null;
            }

            var poppedNode = topTreeStack[^1];
            topTreeStack.RemoveAt(topTreeStack.Count - 1);

            if (topTreeStack.Count == 0)
            {
                treeStacks.RemoveAt(treeStacks.Count - 1);
            }

            return poppedNode;
        }

        // Process tokens
        while (tokens.Count > 0)
        {
            var token = tokens[0].ToUpper();
            tokens.RemoveAt(0);

            switch (token)
            {
                case "ROOT":
                    PushNode(CreateRootNode(tokens, placeholders));
                    break;
                case "SEQUENCE":
                    PushNode(CreateSequenceNode(tokens, placeholders));
                    break;
                case "SELECTOR":
                    PushNode(CreateSelectorNode(tokens, placeholders));
                    break;
                case "PARALLEL":
                    PushNode(CreateParallelNode(tokens, placeholders));
                    break;
                case "RACE":
                    PushNode(CreateRaceNode(tokens, placeholders));
                    break;
                case "ALL":
                    PushNode(CreateAllNode(tokens, placeholders));
                    break;
                case "LOTTO":
                    PushNode(CreateLottoNode(tokens, placeholders));
                    break;
                case "ACTION":
                    PushNode(CreateActionNode(tokens, placeholders));
                    break;
                case "CONDITION":
                    PushNode(CreateConditionNode(tokens, placeholders));
                    break;
                case "WAIT":
                    PushNode(CreateWaitNode(tokens, placeholders));
                    break;
                case "REPEAT":
                    PushNode(CreateRepeatNode(tokens, placeholders));
                    break;
                case "RETRY":
                    PushNode(CreateRetryNode(tokens, placeholders));
                    break;
                case "FLIP":
                    PushNode(CreateFlipNode(tokens, placeholders));
                    break;
                case "SUCCEED":
                    PushNode(CreateSucceedNode(tokens, placeholders));
                    break;
                case "FAIL":
                    PushNode(CreateFailNode(tokens, placeholders));
                    break;
                case "}":
                    var poppedNode = PopNode();
                    if (poppedNode != null)
                    {
                        ValidatePoppedNode(poppedNode);
                    }
                    break;
                default:
                    throw new Exception($"unexpected token: {token}");
            }
        }

        return rootNodes;
    }

    private static RootNodeDefinition CreateRootNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new RootNodeDefinition();

        // Parse arguments and attributes (simplified)
        if (tokens.Count > 0 && tokens[0] != "{")
        {
            // Could have an ID argument
            var args = ParseArgumentTokens(tokens, placeholders);
            if (args.Count == 1 && args[0].Type == "identifier")
            {
                node.Id = args[0].Value?.ToString();
            }
        }

        PopAndCheck(tokens, "{");
        return node;
    }

    private static SequenceNodeDefinition CreateSequenceNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new SequenceNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static SelectorNodeDefinition CreateSelectorNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new SelectorNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static ActionNodeDefinition CreateActionNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var args = ParseArgumentTokens(tokens, placeholders);
        if (args.Count == 0 || args[0].Type != "identifier")
        {
            throw new Exception("expected action name identifier argument");
        }

        // Manual conversion to avoid LINQ allocations
        var nodeArgs = new List<NodeArgument>();
        for (int i = 1; i < args.Count; i++)
        {
            nodeArgs.Add(new NodeArgument { Value = args[i].Value });
        }

        var node = new ActionNodeDefinition
        {
            Call = args[0].Value?.ToString() ?? "",
            Args = nodeArgs.ToArray()
        };

        return node;
    }

    private static ConditionNodeDefinition CreateConditionNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var args = ParseArgumentTokens(tokens, placeholders);
        if (args.Count == 0 || args[0].Type != "identifier")
        {
            throw new Exception("expected condition name identifier argument");
        }

        // Manual conversion to avoid LINQ allocations
        var nodeArgs = new List<NodeArgument>();
        for (int i = 1; i < args.Count; i++)
        {
            nodeArgs.Add(new NodeArgument { Value = args[i].Value });
        }

        var node = new ConditionNodeDefinition
        {
            Call = args[0].Value?.ToString() ?? "",
            Args = nodeArgs.ToArray()
        };

        return node;
    }

    private static ParallelNodeDefinition CreateParallelNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new ParallelNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static RaceNodeDefinition CreateRaceNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new RaceNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static AllNodeDefinition CreateAllNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new AllNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static LottoNodeDefinition CreateLottoNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new LottoNodeDefinition();
        
        // Parse optional weights argument
        if (tokens.Count > 0 && tokens[0] == "[")
        {
            var args = ParseArgumentTokens(tokens, placeholders);
            if (args.Count > 0)
            {
                var weights = new List<double>();
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Type != "number" || !args[i].IsInteger)
                    {
                        throw new Exception("lotto node weight arguments must be positive integer values");
                    }
                    var weight = Convert.ToDouble(args[i].Value);
                    if (weight <= 0)
                    {
                        throw new Exception("lotto node weight arguments must be positive integer values");
                    }
                    weights.Add(weight);
                }
                node.Weights = weights.ToArray();
            }
        }
        
        PopAndCheck(tokens, "{");
        return node;
    }

    private static WaitNodeDefinition CreateWaitNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new WaitNodeDefinition();
        
        // Parse optional duration argument
        if (tokens.Count > 0 && tokens[0] == "[")
        {
            var args = ParseArgumentTokens(tokens, placeholders);
            if (args.Count > 0)
            {
                // All wait node arguments MUST be of type number and must be integer
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Type != "number" || !args[i].IsInteger)
                    {
                        throw new Exception("wait node durations must be integer values");
                    }
                }

                if (args.Count == 1)
                {
                    var duration = Convert.ToInt32(args[0].Value);
                    if (duration <= 0)
                    {
                        throw new Exception("a wait node must have a positive duration");
                    }
                    node.Duration = duration;
                }
                else if (args.Count == 2)
                {
                    var durationMin = Convert.ToInt32(args[0].Value);
                    var durationMax = Convert.ToInt32(args[1].Value);
                    if (durationMin <= 0 || durationMax <= 0)
                    {
                        throw new Exception("a wait node must have a positive minimum and maximum duration");
                    }
                    if (durationMin > durationMax)
                    {
                        throw new Exception("a wait node must not have a minimum duration that exceeds the maximum duration");
                    }
                    node.Duration = new int[] { durationMin, durationMax };
                }
                else
                {
                    throw new Exception("invalid number of wait node duration arguments defined");
                }
            }
        }
        
        return node;
    }

    private static RepeatNodeDefinition CreateRepeatNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new RepeatNodeDefinition();
        
        // Parse optional iterations argument
        if (tokens.Count > 0 && tokens[0] == "[")
        {
            var args = ParseArgumentTokens(tokens, placeholders);
            if (args.Count > 0)
            {
                // All repeat node arguments MUST be of type number and must be integer
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Type != "number" || !args[i].IsInteger)
                    {
                        throw new Exception("repeat node iteration counts must be integer values");
                    }
                }

                if (args.Count == 1)
                {
                    var iterations = Convert.ToInt32(args[0].Value);
                    if (iterations <= 0)
                    {
                        throw new Exception("a repeat node must have a positive number of iterations if defined");
                    }
                    node.Iterations = iterations;
                }
                else if (args.Count == 2)
                {
                    var iterationsMin = Convert.ToInt32(args[0].Value);
                    var iterationsMax = Convert.ToInt32(args[1].Value);
                    if (iterationsMin <= 0 || iterationsMax <= 0)
                    {
                        throw new Exception("a repeat node must have a positive minimum and maximum iteration count if defined");
                    }
                    if (iterationsMin > iterationsMax)
                    {
                        throw new Exception("a repeat node must not have a minimum iteration count that exceeds the maximum iteration count");
                    }
                    node.Iterations = new int[] { iterationsMin, iterationsMax };
                }
                else
                {
                    throw new Exception("invalid number of repeat node iteration count arguments defined");
                }
            }
        }
        
        PopAndCheck(tokens, "{");
        return node;
    }

    private static RetryNodeDefinition CreateRetryNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new RetryNodeDefinition();
        
        // Parse optional attempts argument
        if (tokens.Count > 0 && tokens[0] == "[")
        {
            var args = ParseArgumentTokens(tokens, placeholders);
            if (args.Count > 0)
            {
                // All retry node arguments MUST be of type number and must be integer
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Type != "number" || !args[i].IsInteger)
                    {
                        throw new Exception("retry node attempt counts must be integer values");
                    }
                }

                if (args.Count == 1)
                {
                    var attempts = Convert.ToInt32(args[0].Value);
                    if (attempts <= 0)
                    {
                        throw new Exception("a retry node must have a positive number of attempts if defined");
                    }
                    node.Attempts = attempts;
                }
                else if (args.Count == 2)
                {
                    var attemptsMin = Convert.ToInt32(args[0].Value);
                    var attemptsMax = Convert.ToInt32(args[1].Value);
                    if (attemptsMin <= 0 || attemptsMax <= 0)
                    {
                        throw new Exception("a retry node must have a positive minimum and maximum attempt count if defined");
                    }
                    if (attemptsMin > attemptsMax)
                    {
                        throw new Exception("a retry node must not have a minimum attempt count that exceeds the maximum attempt count");
                    }
                    node.Attempts = new int[] { attemptsMin, attemptsMax };
                }
                else
                {
                    throw new Exception("invalid number of retry node attempt count arguments defined");
                }
            }
        }
        
        PopAndCheck(tokens, "{");
        return node;
    }

    private static FlipNodeDefinition CreateFlipNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new FlipNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static SucceedNodeDefinition CreateSucceedNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new SucceedNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static FailNodeDefinition CreateFailNode(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var node = new FailNodeDefinition();
        PopAndCheck(tokens, "{");
        return node;
    }

    private static List<ArgumentDefinition> ParseArgumentTokens(List<string> tokens, Dictionary<string, string> placeholders)
    {
        var argumentList = new List<ArgumentDefinition>();

        if (tokens.Count == 0 || (tokens[0] != "[" && tokens[0] != "("))
        {
            return argumentList;
        }

        var closingToken = PopAndCheck(tokens, new[] { "[", "(" }) == "[" ? "]" : ")";
        var argumentListTokens = new List<string>();

        while (tokens.Count > 0 && tokens[0] != closingToken)
        {
            argumentListTokens.Add(PopAndCheck(tokens, (string?)null));
        }

        PopAndCheck(tokens, closingToken);

        // Process tokens: each token at even index should be an argument, odd index should be ','
        for (int i = 0; i < argumentListTokens.Count; i++)
        {
            bool shouldBeArgument = (i % 2) == 0;
            
            if (shouldBeArgument)
            {
                argumentList.Add(GetArgumentDefinition(argumentListTokens[i], placeholders));
            }
            else
            {
                // Should be a comma separator
                if (argumentListTokens[i] != ",")
                {
                    throw new Exception($"invalid argument list, expected ',' or '{closingToken}' but got '{argumentListTokens[i]}'");
                }
            }
        }

        return argumentList;
    }

    private static ArgumentDefinition GetArgumentDefinition(string token, Dictionary<string, string> placeholders)
    {
        if (token == "null")
        {
            return new ArgumentDefinition { Value = null, Type = "null" };
        }

        if (token == "true" || token == "false")
        {
            return new ArgumentDefinition { Value = token == "true", Type = "boolean" };
        }

        if (double.TryParse(token, out var number))
        {
            return new ArgumentDefinition
            {
                Value = number,
                Type = "number",
                IsInteger = number == (int)number
            };
        }

        if (placeholders.ContainsKey(token))
        {
            return new ArgumentDefinition
            {
                Value = placeholders[token].Replace("\\\"", "\""),
                Type = "string"
            };
        }

        if (token.StartsWith("$") && token.Length > 1)
        {
            return new ArgumentDefinition
            {
                Value = token.Substring(1),
                Type = "property_reference"
            };
        }

        return new ArgumentDefinition { Value = token, Type = "identifier" };
    }

    private static string PopAndCheck(List<string> tokens, string? expected = null)
    {
        return PopAndCheck(tokens, expected != null ? new[] { expected } : null);
    }

    private static string PopAndCheck(List<string> tokens, string[]? expected = null)
    {
        if (tokens.Count == 0)
        {
            throw new Exception("unexpected end of definition");
        }

        var popped = tokens[0];
        tokens.RemoveAt(0);

        if (expected != null && expected.Length > 0)
        {
            // Manual check to avoid LINQ
            bool found = false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i].Equals(popped, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new Exception($"unexpected token found. Expected {string.Join(" or ", expected)} but got '{popped}'");
            }
        }

        return popped;
    }

    private static void ValidatePoppedNode(NodeDefinition definition)
    {
        if (definition is DecoratorNodeDefinition decorator && decorator.Child == null)
        {
            throw new Exception($"a {definition.Type} node must have a single child node defined");
        }

        if (definition is CompositeNodeDefinition composite && (composite.Children == null || composite.Children.Count == 0))
        {
            throw new Exception($"a {definition.Type} node must have at least a single child node defined");
        }
    }

    private class TokeniseResult
    {
        public List<string> Tokens { get; set; } = new();
        public Dictionary<string, string> Placeholders { get; set; } = new();
    }

    private class StringLiteralSubstitutionResult
    {
        public Dictionary<string, string> Placeholders { get; set; } = new();
        public string ProcessedDefinition { get; set; } = "";
    }

    private class ArgumentDefinition
    {
        public object? Value { get; set; }
        public string Type { get; set; } = "";
        public bool IsInteger { get; set; }
    }
}

