namespace TimeoutPattern.Application.Reports;

public sealed record DailySales(DateOnly Day, int Orders, decimal Revenue);

/// <param name="IsPartial">True when the deadline ended the report early; the totals cover only <paramref name="DaysCompleted"/> days.</param>
public sealed record SalesReport(
    int DaysRequested,
    int DaysCompleted,
    long Orders,
    decimal Revenue,
    bool IsPartial,
    string? StopReason,
    double ForecastPerDayMs);
