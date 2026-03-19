using System;
using System.Reflection;

namespace Mistreevous;

/// <summary>
/// A singleton used to store and lookup registered functions and subtrees.
/// </summary>
public static class Lookup
{
    /// <summary>
    /// The dictionary holding any registered functions keyed on function name.
    /// </summary>
    private static readonly Dictionary<string, GlobalFunction> _registeredFunctions = new();

    /// <summary>
    /// The dictionary holding any registered subtree root node definitions keyed on tree name.
    /// </summary>
    private static readonly Dictionary<string, RootNodeDefinition> _registeredSubtrees = new();

    /// <summary>
    /// Gets the function with the specified name.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <returns>The function with the specified name, or null if not found.</returns>
    public static GlobalFunction? GetFunc(string name)
    {
        return _registeredFunctions.TryGetValue(name, out var func) ? func : null;
    }

    /// <summary>
    /// Sets the function with the specified name for later lookup.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="func">The function.</param>
    public static void SetFunc(string name, GlobalFunction func)
    {
        _registeredFunctions[name] = func;
    }

    /// <summary>
    /// Gets the function invoker for the specified agent and function name.
    /// If a function with the specified name exists on the agent object then it will
    /// be returned, otherwise we will then check the registered functions for a match.
    /// </summary>
    /// <param name="agent">The agent instance that this behaviour tree is modelling behaviour for.</param>
    /// <param name="name">The function name.</param>
    /// <returns>The function invoker for the specified agent and function name, or null if not found.</returns>
    public static Func<object?[], object?>? GetFuncInvoker(IAgent agent, string name)
    {
        // Process any arguments that will be passed to an agent or registered function.
        object?[] ProcessFunctionArguments(object?[] args)
        {
            // Manual conversion to avoid LINQ allocations
            var processedArgs = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                // This argument may be an agent property reference. If it is we should substitute it for the value of the agent property it references.
                // An agent property reference will be an object with a single "$" property with a string value representing the agent property name.
                if (arg is Dictionary<string, object?> dict && dict.Count == 1 && dict.ContainsKey("$"))
                {
                    var agentPropertyName = dict["$"]?.ToString();
                    if (string.IsNullOrEmpty(agentPropertyName))
                    {
                        throw new Exception("Agent property reference must be a string");
                    }
                    processedArgs[i] = agent[agentPropertyName];
                }
                else
                {
                    // The argument can be passed to the function as-is.
                    processedArgs[i] = arg;
                }
            }
            return processedArgs;
        }

        // Check whether the agent contains the specified function using reflection.
        var agentType = agent.GetType();
        // First try exact match (case-sensitive)
        var method = agentType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        
        // If not found, try case-insensitive
        if (method == null)
        {
            var methods = agentType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                if (string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    method = m;
                    break;
                }
            }
        }
        
        if (method != null)
        {
            return (args) =>
            {
                var processedArgs = ProcessFunctionArguments(args);
                var parameters = method.GetParameters();
                
                // Convert arguments to match method parameters
                var methodArgs = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    
                    if (i < processedArgs.Length)
                    {
                        // Try to convert the argument to the parameter type if needed
                        var argValue = processedArgs[i];
                        
                        if (argValue != null && !paramType.IsAssignableFrom(argValue.GetType()))
                        {
                            // Try to convert the type
                            if (paramType.IsEnum && argValue is string stringValue)
                            {
                                methodArgs[i] = Enum.Parse(paramType, stringValue, true);
                            }
                            else if (paramType == typeof(int) && argValue is double doubleValue)
                            {
                                methodArgs[i] = (int)doubleValue;
                            }
                            else
                            {
                                methodArgs[i] = Convert.ChangeType(argValue, paramType);
                            }
                        }
                        else
                        {
                            methodArgs[i] = argValue;
                        }
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        methodArgs[i] = parameters[i].DefaultValue;
                    }
                    else if (paramType.IsValueType)
                    {
                        methodArgs[i] = Activator.CreateInstance(paramType);
                    }
                    else
                    {
                        methodArgs[i] = null;
                    }
                }
                
                // Invoke the method
                var result = method.Invoke(agent, methodArgs);
                return result;
            };
        }

        // Check if it's a property that returns a GlobalFunction delegate
        var agentProperty = agent[name];
        if (agentProperty is GlobalFunction agentFunction)
        {
            return (args) => agentFunction(agent, ProcessFunctionArguments(args));
        }

        // The agent does not contain the specified function but it may have been registered at some point.
        if (_registeredFunctions.TryGetValue(name, out var registeredFunction))
        {
            return (args) => registeredFunction(agent, ProcessFunctionArguments(args));
        }

        // We have no function to invoke.
        return null;
    }

    /// <summary>
    /// Gets all registered subtree root node definitions.
    /// </summary>
    public static IReadOnlyDictionary<string, RootNodeDefinition> GetSubtrees()
    {
        return _registeredSubtrees;
    }

    /// <summary>
    /// Sets the subtree with the specified name for later lookup.
    /// </summary>
    /// <param name="name">The name of the subtree.</param>
    /// <param name="subtree">The subtree.</param>
    public static void SetSubtree(string name, RootNodeDefinition subtree)
    {
        _registeredSubtrees[name] = subtree;
    }

    /// <summary>
    /// Removes the registered function or subtree with the specified name.
    /// </summary>
    /// <param name="name">The name of the registered function or subtree.</param>
    public static void Remove(string name)
    {
        _registeredFunctions.Remove(name);
        _registeredSubtrees.Remove(name);
    }

    /// <summary>
    /// Remove all registered functions and subtrees.
    /// </summary>
    public static void Empty()
    {
        _registeredFunctions.Clear();
        _registeredSubtrees.Clear();
    }
}

