namespace Mistreevous;

/// <summary>
/// A REPEAT node.
/// The node has a single child which can have:
/// -- A number of iterations for which to repeat the child node.
/// -- An infinite repeat loop if neither an iteration count or a condition function is defined.
/// The REPEAT node will stop and have a 'FAILED' state if its child is ever in a 'FAILED' state after an update.
/// The REPEAT node will attempt to move on to the next iteration if its child is ever in a 'SUCCEEDED' state.
/// </summary>
public class Repeat : Decorator
{
    private readonly int? _iterations;
    private readonly int? _iterationsMin;
    private readonly int? _iterationsMax;

    /// <summary>
    /// Creates a new instance of the Repeat class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="iterations">The number of iterations to repeat the child node.</param>
    /// <param name="iterationsMin">The minimum possible number of iterations to repeat the child node.</param>
    /// <param name="iterationsMax">The maximum possible number of iterations to repeat the child node.</param>
    /// <param name="child">The child node.</param>
    public Repeat(List<Attribute> attributes, BehaviourTreeOptions options, int? iterations, int? iterationsMin, int? iterationsMax, Node child)
        : base("repeat", attributes, options, child)
    {
        _iterations = iterations;
        _iterationsMin = iterationsMin;
        _iterationsMax = iterationsMax;
    }

    /// <summary>
    /// The number of target iterations to make.
    /// </summary>
    private int? _targetIterationCount = null;

    /// <summary>
    /// The current iteration count.
    /// </summary>
    private int _currentIterationCount = 0;

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If this node is in the READY state then we need to reset the child and the target iteration count.
        if (Is(State.Ready))
        {
            // Reset the child node.
            Child.Reset();

            // Reset the current iteration count.
            _currentIterationCount = 0;

            // Set the target iteration count.
            SetTargetIterationCount();
        }

        // Do a check to see if we can iterate. If we can then this node will move into the 'RUNNING' state.
        // If we cannot iterate then we have hit our target iteration count, which means that the node has succeeded.
        if (CanIterate())
        {
            // This node is in the running state and can do its initial iteration.
            SetState(State.Running);

            // We may have already completed an iteration, meaning that the child node will be in the SUCCEEDED state.
            // If this is the case then we will have to reset the child node now.
            if (Child.GetState() == State.Succeeded)
            {
                Child.Reset();
            }

            // Update the child of this node.
            Child.Update(agent);

            // If the child moved into the FAILED state when we updated it then there is nothing left to do and this node has also failed.
            // If it has moved into the SUCCEEDED state then we have completed the current iteration.
            if (Child.GetState() == State.Failed)
            {
                // The child has failed, meaning that this node has failed.
                SetState(State.Failed);

                return;
            }
            else if (Child.GetState() == State.Succeeded)
            {
                // We have completed an iteration.
                _currentIterationCount += 1;
            }
        }
        else
        {
            // This node is in the 'SUCCEEDED' state as we cannot iterate any more.
            SetState(State.Succeeded);
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName()
    {
        if (_iterations != null)
        {
            return $"REPEAT {_iterations}x";
        }
        else if (_iterationsMin != null && _iterationsMax != null)
        {
            return $"REPEAT {_iterationsMin}x-{_iterationsMax}x";
        }
        else
        {
            return "REPEAT";
        }
    }

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public override void Reset()
    {
        // Reset the state of this node.
        SetState(State.Ready);

        // Reset the current iteration count.
        _currentIterationCount = 0;

        // Reset the child node.
        Child.Reset();
    }

    /// <summary>
    /// Gets whether an iteration can be made.
    /// </summary>
    /// <returns>Whether an iteration can be made.</returns>
    private bool CanIterate()
    {
        if (_targetIterationCount != null)
        {
            // We can iterate as long as we have not reached our target iteration count.
            return _currentIterationCount < _targetIterationCount.Value;
        }

        // If neither an iteration count or a condition function were defined then we can iterate indefinitely.
        return true;
    }

    /// <summary>
    /// Sets the target iteration count.
    /// </summary>
    private void SetTargetIterationCount()
    {
        // Are we dealing with an explicit iteration count or will we be randomly picking an iteration count between the min and max iteration count.
        if (_iterations != null)
        {
            _targetIterationCount = _iterations;
        }
        else if (_iterationsMin != null && _iterationsMax != null)
        {
            // We will be picking a random iteration count between a min and max iteration count, if the optional 'random'
            // behaviour tree function option is defined then we will be using that, otherwise we will fall back to using Random.NextDouble.
            var random = Options.Random ?? (() => new Random().NextDouble());

            // Pick a random iteration count between a min and max iteration count.
            _targetIterationCount = (int)Math.Floor(
                random() * (_iterationsMax.Value - _iterationsMin.Value + 1) + _iterationsMin.Value
            );
        }
        else
        {
            _targetIterationCount = null;
        }
    }
}

