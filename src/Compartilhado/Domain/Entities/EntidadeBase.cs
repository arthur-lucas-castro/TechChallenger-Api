namespace Compartilhado.Domain.Entities
{
    public abstract class EntidadeBase
    {
        public int Id { get; set; }

        private readonly List<IDomainEvent> _domainEvents = [];

        public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        protected void AddDomainEvent(IDomainEvent evento) => _domainEvents.Add(evento);
    }
}
