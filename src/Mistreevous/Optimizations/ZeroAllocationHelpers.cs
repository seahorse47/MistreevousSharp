using System.Buffers;

namespace Mistreevous.Optimizations;

/// <summary>
/// Helper methods for zero-allocation operations.
/// Based on techniques from: https://andrebaltieri.com/zero-allocation-techniques-in-csharp-using-ref-struct-and-readonly-struct/
/// </summary>
internal static class ZeroAllocationHelpers
{
    /// <summary>
    /// Gets attributes of a specific type without LINQ allocations.
    /// </summary>
    public static T? GetAttributeOfType<T>(Entry? entry, Step? step, Exit? exit, While? whileGuard, Until? until) where T : class
    {
        if (entry is T entryResult) return entryResult;
        if (step is T stepResult) return stepResult;
        if (exit is T exitResult) return exitResult;
        if (whileGuard is T whileResult) return whileResult;
        if (until is T untilResult) return untilResult;
        return null;
    }

    /// <summary>
    /// Collects non-null attributes into a list without LINQ.
    /// </summary>
    public static void CollectAttributes(Entry? entry, Step? step, Exit? exit, While? whileGuard, Until? until, List<Attribute> output)
    {
        output.Clear();
        if (entry != null) output.Add(entry);
        if (step != null) output.Add(step);
        if (exit != null) output.Add(exit);
        if (whileGuard != null) output.Add(whileGuard);
        if (until != null) output.Add(until);
    }

    /// <summary>
    /// Counts items matching a condition without LINQ.
    /// </summary>
    public static int CountWhere<T>(IReadOnlyList<T> items, Func<T, bool> predicate)
    {
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (predicate(items[i]))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Rents an array from the pool and returns it when done.
    /// Based on: https://github.com/nazarovsa/csharp-zero-allocation
    /// </summary>
    public static T[] RentArray<T>(int minimumLength)
    {
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Returns an array to the pool.
    /// </summary>
    public static void ReturnArray<T>(T[] array)
    {
        if (array != null)
        {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

