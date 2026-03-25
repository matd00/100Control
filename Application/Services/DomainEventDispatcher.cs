using Domain.Common;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            // For now, we'll just log or use DI to find handlers if we had them.
            // When we add MediatR, this will just be _mediator.Publish(domainEvent).
            System.Diagnostics.Debug.WriteLine($"Dispatching Domain Event: {domainEvent.GetType().Name} occurred at {domainEvent.OccurredOn}");
        }
        await Task.CompletedTask;
    }
}
