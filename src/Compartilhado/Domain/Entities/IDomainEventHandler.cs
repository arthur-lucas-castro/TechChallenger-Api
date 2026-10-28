namespace Compartilhado.Domain.Entities
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
    }
}
