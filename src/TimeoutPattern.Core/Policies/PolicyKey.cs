namespace TimeoutPattern.Core.Policies;

internal static class PolicyKey
{
    /// <summary>Yields a dotted key and each of its ancestors, most specific first: <c>a.b.c</c>, <c>a.b</c>, <c>a</c>.</summary>
    public static IEnumerable<string> Lineage(string key)
    {
        for (var current = key; current.Length > 0;)
        {
            yield return current;

            var separator = current.LastIndexOf('.');
            current = separator < 0 ? string.Empty : current[..separator];
        }
    }
}
