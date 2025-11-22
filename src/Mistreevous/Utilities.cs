namespace Mistreevous;

/// <summary>
/// Utility functions for the behaviour tree library.
/// </summary>
public static class Utilities
{
    /// <summary>
    /// Create a randomly generated node uid.
    /// </summary>
    /// <returns>A randomly generated node uid.</returns>
    public static string CreateUid()
    {
        var s4 = () => ((int)((1 + new Random().NextDouble()) * 0x10000) | 0).ToString("x").Substring(1);
        return $"{s4()}{s4()}-{s4()}-{s4()}-{s4()}-{s4()}{s4()}{s4()}";
    }
}

