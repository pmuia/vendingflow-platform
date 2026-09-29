using MachineService.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace MachineService.API.Controllers;

public sealed class MachinesController(ILogger<MachinesController> logger) : ApiBaseController
{
    [HttpPost]
    [Route("api/machines")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenericRequest([FromBody] GenericRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Machine command received Service={Service}", request.Service);
            return Ok(await Mediator.Send(CommandFactory.Create(request), cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Machine command failed Service={Service}", request.Service);
            return BadRequest(ResponseModel.Fail(ex.Message));
        }
    }
}
