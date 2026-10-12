using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TimeoutPattern.Api.Timeouts;
using TimeoutPattern.Application.Reports;

namespace TimeoutPattern.Api.Controllers;

[ApiController]
[Route("api/reports")]
[TimeoutPolicy("reports.sales")]
public sealed class ReportsController(SalesReportService reports) : ControllerBase
{
    /// <summary>Aggregates daily sales until done, or until the forecast says the next day would miss the deadline.</summary>
    [HttpGet("sales")]
    [ProducesResponseType<SalesReport>(StatusCodes.Status200OK)]
    public Task<SalesReport> Sales([FromQuery, Range(1, 3650)] int days, CancellationToken cancellationToken) =>
        reports.GenerateAsync(days, cancellationToken);
}
