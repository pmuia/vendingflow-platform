using InventoryService.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.API.Controllers;

public sealed class InventoryController(ILogger<InventoryController> logger) : ApiBaseController
{
    [HttpPost]
    [Route("api/inventory")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenericRequest([FromBody] GenericRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Inventory command received Service={Service}", request.Service);
            return Ok(await Mediator.Send(CommandFactory.Create(request), cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inventory command failed Service={Service}", request.Service);
            return BadRequest(ResponseModel.Fail(ex.Message));
        }
    }
}
