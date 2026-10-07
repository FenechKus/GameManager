namespace GameManager.Core.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<object> domainEvents);
}