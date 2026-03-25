using Domain.Common;

namespace Domain.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents);
}
