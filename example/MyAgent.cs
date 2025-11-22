using Mistreevous;

public class MyAgent : IAgent
{
    public object? this[string propertyName] => 
        GetType().GetProperty(propertyName)?.GetValue(this);

    public State Walk()
    {
        Console.WriteLine("walking!");
        return State.Succeeded;
    }

    public State Fall()
    {
        Console.WriteLine("falling!");
        return State.Succeeded;
    }

    public State Laugh()
    {
        Console.WriteLine("laughing!");
        return State.Succeeded;
    }
}