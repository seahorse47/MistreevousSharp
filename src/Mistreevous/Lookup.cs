using System;
using System.Reflection;

namespace Mistreevous;

/// <summary>
/// An interface for looking up agent functions.
/// </summary>
public interface ILookup
{
    /// <summary>
    /// Gets the function invoker for the specified agent.
    /// </summary>
    /// <param name="agent">The agent instance that this behaviour tree is modelling behaviour for.</param>
    /// <param name="name">The function name.</param>
    /// <returns>The function invoker for the specified agent, or null if not found.</returns>
    Func<object?[], object?>? GetFuncInvoker(IAgent agent, string name);
}

/// <summary>
/// Default implementation of `ILookup` interface.
/// </summary>
public class Lookup : ILookup
{
    /// <summary>
    /// The default lookup instance.
    /// It's used to store functions and subtrees registered through relevant methods of `BehaviourTree`.
    /// </summary>
    public static readonly Lookup Default = new Lookup();

    /// <summary>
    /// The dictionary holding any registered functions keyed on function name.
    /// </summary>
    private readonly Dictionary<string, GlobalFunction> _registeredFunctions = new();

    /// <summary>
    /// The dictionary holding any registered subtree root node definitions keyed on tree name.
    /// </summary>
    private readonly Dictionary<string, RootNodeDefinition> _registeredSubtrees = new();

    /// <summary>
    /// Gets the function with the specified name.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <returns>The function with the specified name, or null if not found.</returns>
    public GlobalFunction? GetFunc(string name)
    {
        return _registeredFunctions.TryGetValue(name, out var func) ? func : null;
    }

    /// <summary>
    /// Sets the function with the specified name for later lookup.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="func">The function.</param>
    public void SetFunc(string name, GlobalFunction func)
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
    public Func<object?[], object?>? GetFuncInvoker(IAgent agent, string name)
    {
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
            var parameters = method.GetParameters();
            var methodArgs = new object?[parameters.Length];

            return (args) =>
            {
                // Convert arguments to match method parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (i < args.Length)
                    {
                        // Try to convert the argument to the parameter type if needed
                        var argValue = args[i];
                        
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
            return (args) => agentFunction(agent, args);
        }

        // The agent does not contain the specified function but it may have been registered at some point.
        if (_registeredFunctions.TryGetValue(name, out var registeredFunction))
        {
            return (args) => registeredFunction(agent, args);
        }

        // We have no function to invoke.
        return null;
    }

    /// <summary>
    /// Gets all registered subtree root node definitions.
    /// </summary>
    public IReadOnlyDictionary<string, RootNodeDefinition> GetSubtrees()
    {
        return _registeredSubtrees;
    }

    /// <summary>
    /// Sets the subtree with the specified name for later lookup.
    /// </summary>
    /// <param name="name">The name of the subtree.</param>
    /// <param name="subtree">The subtree.</param>
    public void SetSubtree(string name, RootNodeDefinition subtree)
    {
        _registeredSubtrees[name] = subtree;
    }

    /// <summary>
    /// Removes the registered function or subtree with the specified name.
    /// </summary>
    /// <param name="name">The name of the registered function or subtree.</param>
    public void Remove(string name)
    {
        _registeredFunctions.Remove(name);
        _registeredSubtrees.Remove(name);
    }

    /// <summary>
    /// Remove all registered functions and subtrees.
    /// </summary>
    public void Empty()
    {
        _registeredFunctions.Clear();
        _registeredSubtrees.Clear();
    }
}

