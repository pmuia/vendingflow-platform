using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Interfaces;

namespace PaymentService.API.Controllers;

[ApiController]
public abstract class ApiBaseController : ControllerBase
{
    private ISender? _mediator;
    private ICommandFactory? _commandFactory;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICommandFactory CommandFactory => _commandFactory ??= HttpContext.RequestServices.GetRequiredService<ICommandFactory>();
}
