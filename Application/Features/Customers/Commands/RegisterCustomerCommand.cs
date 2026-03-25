using Domain.Common;
using MediatR;

namespace Application.Features.Customers.Commands;

public class RegisterCustomerCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Complement { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
