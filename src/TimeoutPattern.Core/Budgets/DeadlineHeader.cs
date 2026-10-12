using System.Globalization;

namespace TimeoutPattern.Core.Budgets;

/// <summary>Wire format of a propagated deadline: remaining whole milliseconds as an invariant integer.</summary>
public static class DeadlineHeader
{
    private static readonly long s_maxMilliseconds = (long)TimeSpan.FromDays(1).TotalMilliseconds;

    public static string Format(TimeSpan remaining) =>
        ((long)Math.Max(0, Math.Floor(remaining.TotalMilliseconds))).ToString(CultureInfo.InvariantCulture);

    public static bool TryParse(string? value, out TimeSpan deadline)
    {
        if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds <= s_maxMilliseconds)
        {
            deadline = TimeSpan.FromMilliseconds(milliseconds);
            return true;
        }

        deadline = default;
        return false;
    }
}
