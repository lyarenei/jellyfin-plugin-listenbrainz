namespace Jellyfin.Plugin.ListenBrainz.Common.Extensions;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Filter values which are null and return <see cref="IEnumerable{T}"/> of a non-nullable type.
    /// From: https://codereview.stackexchange.com/a/283504.
    /// </summary>
    /// <param name="source">Source enumerable.</param>
    /// <typeparam name="T">Type of enumerable element.</typeparam>
    /// <returns>Enumerable of non-nullable type.</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        foreach (var item in source)
        {
            if (item is { } notNullItem)
            {
                yield return notNullItem;
            }
        }
    }
}
