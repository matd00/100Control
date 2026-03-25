using Domain.Common;
using MediatR;

namespace Application.Features.Orders.Commands;

public class CreateOrderCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; set; }
    public Domain.Entities.OrderSource Source { get; set; }
    public bool IsDropshipping { get; set; }
    public List<CreateOrderItemCommand> Items { get; set; } = new();
}

public class CreateOrderItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
