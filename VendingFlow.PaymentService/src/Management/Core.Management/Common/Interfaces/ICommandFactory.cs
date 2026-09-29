using MediatR;
using PaymentService.Application.Common.Models;

namespace PaymentService.Application.Common.Interfaces;

public interface ICommandFactory
{
    IRequest<ResponseModel> Create(GenericRequest request);
}
