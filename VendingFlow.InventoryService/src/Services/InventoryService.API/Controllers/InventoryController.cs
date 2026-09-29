using InventoryService.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.API.Controllers;

[Route("api/inventory")]
public class InventoryController : ApiBaseController
{
    [HttpPost]
    public async Task<ResponseModel> Execute([FromBody] GenericRequest request, CancellationToken cancellationToken)
    {
        var command = CommandFactory.Create(request);
        return await Mediator.Send(command, cancellationToken);
    }
}
