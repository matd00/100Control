using FluentValidation;

namespace Application.Features.Orders.Commands;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Cliente é obrigatório");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("O pedido deve ter pelo menos um item");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Produto é obrigatório");
            items.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");
        });
    }
}
