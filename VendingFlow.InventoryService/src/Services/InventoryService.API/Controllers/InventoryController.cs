using InventoryService.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.API.Controllers;

[Route("api/inventory")]
public class InventoryController : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<ResponseModel>> Execute([FromBody] GenericRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = CommandFactory.Create(request);
            return Ok(await Mediator.Send(command, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(ResponseModel.Fail(exception.Message));
        }
    }
}
