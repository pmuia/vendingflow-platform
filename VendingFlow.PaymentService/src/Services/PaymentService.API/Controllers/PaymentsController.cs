using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Models;

namespace PaymentService.API.Controllers;

[Route("api/payments")]
public class PaymentsController : ApiBaseController
{
    [HttpPost]
    public async Task<ResponseModel> Execute([FromBody] GenericRequest request, CancellationToken cancellationToken)
    {
        var command = CommandFactory.Create(request);
        return await Mediator.Send(command, cancellationToken);
    }
}
