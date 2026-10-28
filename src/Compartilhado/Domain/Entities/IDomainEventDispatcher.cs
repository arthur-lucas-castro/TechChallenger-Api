namespace Compartilhado.Domain.Entities
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    }
}
