namespace Mistreevous;

/// <summary>
/// A WAIT node.
/// The state of this node will change to SUCCEEDED after a duration of time.
/// </summary>
public class Wait : Leaf
{
    private readonly int? _duration;
    private readonly int? _durationMin;
    private readonly int? _durationMax;

    /// <summary>
    /// Creates a new instance of the Wait class.
    /// </summary>
    /// <param name="attributes">The node attributes.</param>
    /// <param name="options">The behaviour tree options.</param>
    /// <param name="duration">The duration that this node will wait to succeed in milliseconds.</param>
    /// <param name="durationMin">The minimum possible duration in milliseconds that this node will wait to succeed.</param>
    /// <param name="durationMax">The maximum possible duration in milliseconds that this node will wait to succeed.</param>
    public Wait(List<Attribute> attributes, BehaviourTreeOptions options, int? duration, int? durationMin, int? durationMax)
        : base("wait", attributes, options)
    {
        _duration = duration;
        _durationMin = durationMin;
        _durationMax = durationMax;
    }

    /// <summary>
    /// The time in milliseconds at which this node was first updated.
    /// </summary>
    private long _initialUpdateTime = 0;

    /// <summary>
    /// The total duration in milliseconds that this node will be waiting for.
    /// </summary>
    private int? _totalDuration = null;

    /// <summary>
    /// The duration in milliseconds that this node has been waiting for.
    /// </summary>
    private double _waitedDuration = 0;

    /// <summary>
    /// Called when the node is being updated.
    /// </summary>
    /// <param name="agent">The agent.</param>
    protected override void OnUpdate(IAgent agent)
    {
        // If this node is in the READY state then we need to set the initial update time.
        if (Is(State.Ready))
        {
            // Set the initial update time.
            _initialUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Set the initial waited duration.
            _waitedDuration = 0;

            // Are we dealing with an explicit duration or will we be randomly picking a duration between the min and max duration.
            if (_duration != null)
            {
                _totalDuration = _duration;
            }
            else if (_durationMin != null && _durationMax != null)
            {
                // We will be picking a random duration between a min and max duration, if the optional 'random' behaviour tree
                // function option is defined then we will be using that, otherwise we will fall back to using Random.NextDouble.
                var random = Options.Random ?? (() => new Random().NextDouble());

                // Pick a random duration between a min and max duration.
                _totalDuration = (int)Math.Floor(
                    random() * (_durationMax.Value - _durationMin.Value + 1) + _durationMin.Value
                );
            }
            else
            {
                _totalDuration = null;
            }

            // The node is now running until we finish waiting.
            SetState(State.Running);
        }

        // If we have no total duration then this wait node will wait indefinitely until it is aborted.
        if (_totalDuration == null)
        {
            return;
        }

        // If we have a 'getDeltaTime' function defined as part of our options then we will use it to figure out how long we have waited for.
        if (Options.GetDeltaTime != null)
        {
            // Get the delta time.
            var deltaTime = Options.GetDeltaTime();

            // Our delta time must be a valid number and cannot be NaN.
            if (double.IsNaN(deltaTime))
            {
                throw new Exception("The delta time must be a valid number and not NaN.");
            }

            // Update the amount of time that this node has been waiting for based on the delta time.
            _waitedDuration += deltaTime * 1000;
        }
        else
        {
            // We are not using a delta time, so we will just work out how much time has passed since the first update.
            _waitedDuration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _initialUpdateTime;
        }

        // Have we waited long enough?
        if (_waitedDuration >= _totalDuration.Value)
        {
            // We have finished waiting!
            SetState(State.Succeeded);
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public override string GetName()
    {
        if (_duration != null)
        {
            return $"WAIT {_duration}ms";
        }
        else if (_durationMin != null && _durationMax != null)
        {
            return $"WAIT {_durationMin}ms-{_durationMax}ms";
        }
        else
        {
            return "WAIT";
        }
    }
}

