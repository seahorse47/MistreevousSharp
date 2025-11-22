namespace Mistreevous;

/// <summary>
/// A RETRY node.
/// The node has a single child which can have:
/// -- A number of iterations for which to repeat the child node.
/// -- An infinite repeat loop if neither an iteration count or a condition function is defined.
/// The RETRY node will stop and have a 'SUCCEEDED' state if its child is ever in a 'SUCCEEDED' state after an update.
/// The RETRY node will attempt to move on to the next iteration if its child is ever in a 'FAILED' state.
/// </summary>
public class Retry : Decorator
{
    private readonly int? _attempts;
    private readonly int? _attemptsMin;
    private readonly int? _attemptsMax;

    /// <summary>
    /// Creates a new instance of the Retry class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="attempts">The number of attempts to retry the child node.</param>
    /// <param name="attemptsMin">The minimum possible number of attempts to retry the child node.</param>
    /// <param name="attemptsMax">The maximum possible number of attempts to retry the child node.</param>
    /// <param name="child">The child node.</param>
    public Retry(List<Attribute> attributes, BehaviourTreeOptions options, int? attempts, int? attemptsMin, int? attemptsMax, Node child)
        : base("retry", attributes, options, child)
    {
        _attempts = attempts;
        _attemptsMin = attemptsMin;
        _attemptsMax = attemptsMax;
    }

    /// <summary>
    /// The number of target attempts to make.
    /// </summary>
    private int? _targetAttemptCount = null;

    /// <summary>
    /// The current attempt count.
    /// </summary>
    private int _currentAttemptCount = 0;

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If this node is in the READY state then we need to reset the child and the target attempt count.
        if (Is(State.Ready))
        {
            // Reset the child node.
            Child.Reset();

            // Reset the current attempt count.
            _currentAttemptCount = 0;

            // Set the target attempt count.
            SetTargetAttemptCount();
        }

        // Do a check to see if we can attempt. If we can then this node will move into the 'RUNNING' state.
        // If we cannot attempt then we have hit our target attempt count, which means that the node has failed.
        if (CanAttempt())
        {
            // This node is in the running state and can do its initial attempt.
            SetState(State.Running);

            // We may have already completed an attempt, meaning that the child node will be in the FAILED state.
            // If this is the case then we will have to reset the child node now.
            if (Child.GetState() == State.Failed)
            {
                Child.Reset();
            }

            // Update the child of this node.
            Child.Update(agent);

            // If the child moved into the SUCCEEDED state when we updated it then there is nothing left to do and this node has also succeeded.
            // If it has moved into the FAILED state then we have completed the current attempt.
            if (Child.GetState() == State.Succeeded)
            {
                // The child has succeeded, meaning that this node has succeeded.
                SetState(State.Succeeded);

                return;
            }
            else if (Child.GetState() == State.Failed)
            {
                // We have completed an attempt.
                _currentAttemptCount += 1;
            }
        }
        else
        {
            // This node is in the 'FAILED' state as we cannot attempt any more.
            SetState(State.Failed);
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName()
    {
        if (_attempts != null)
        {
            return $"RETRY {_attempts}x";
        }
        else if (_attemptsMin != null && _attemptsMax != null)
        {
            return $"RETRY {_attemptsMin}x-{_attemptsMax}x";
        }
        else
        {
            return "RETRY";
        }
    }

    /// <summary>
    /// Reset the state of the node.
    /// </summary>
    public override void Reset()
    {
        // Reset the state of this node.
        SetState(State.Ready);

        // Reset the current attempt count.
        _currentAttemptCount = 0;

        // Reset the child node.
        Child.Reset();
    }

    /// <summary>
    /// Gets whether an attempt can be made.
    /// </summary>
    /// <returns>Whether an attempt can be made.</returns>
    private bool CanAttempt()
    {
        if (_targetAttemptCount != null)
        {
            // We can attempt as long as we have not reached our target attempt count.
            return _currentAttemptCount < _targetAttemptCount.Value;
        }

        // If neither an attempt count or a condition function were defined then we can attempt indefinitely.
        return true;
    }

    /// <summary>
    /// Sets the target attempt count.
    /// </summary>
    private void SetTargetAttemptCount()
    {
        // Are we dealing with an explicit attempt count or will we be randomly picking an attempt count between the min and max attempt count.
        if (_attempts != null)
        {
            _targetAttemptCount = _attempts;
        }
        else if (_attemptsMin != null && _attemptsMax != null)
        {
            // We will be picking a random attempt count between a min and max attempt count, if the optional 'random'
            // behaviour tree function option is defined then we will be using that, otherwise we will fall back to using Random.NextDouble.
            var random = Options.Random ?? (() => new Random().NextDouble());

            // Pick a random attempt count between a min and max attempt count.
            _targetAttemptCount = (int)Math.Floor(
                random() * (_attemptsMax.Value - _attemptsMin.Value + 1) + _attemptsMin.Value
            );
        }
        else
        {
            _targetAttemptCount = null;
        }
    }
}

