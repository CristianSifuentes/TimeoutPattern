using Microsoft.AspNetCore.Mvc;
using TimeoutPattern.Api.Timeouts;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Api.Controllers;

[ApiController]
[Route("api/products")]
[TimeoutPolicy("products.read")]
public sealed class ProductsController(ProductPageService pages) : ControllerBase
{
    /// <summary>Product page: catalog and pricing are required, inventory degrades gracefully.</summary>
    [HttpGet("{sku}")]
    [ProducesResponseType<ProductPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ProductPage>> Get(string sku, CancellationToken cancellationToken) =>
        await pages.FindAsync(sku, cancellationToken) is { } page ? page : NotFound();
}
