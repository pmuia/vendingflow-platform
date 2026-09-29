using MediatR;
using PaymentService.Application.Common.Attributes;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Services.PaymentManagement.Models;

namespace PaymentService.Application.Services.PaymentManagement.Queries;

[CommandName("getRecentPayments")]
public record GetRecentPaymentsQuery : IRequest<ResponseModel>;

public class GetRecentPaymentsQueryHandler(IPaymentRepository repository) : IRequestHandler<GetRecentPaymentsQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetRecentPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await repository.RecentAsync(cancellationToken);
        return ResponseModel.Ok(payments.Select(payment => payment.ToDto()).ToList());
    }
}
