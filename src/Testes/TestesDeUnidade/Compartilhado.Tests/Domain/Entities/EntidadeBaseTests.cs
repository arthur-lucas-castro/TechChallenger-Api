using Compartilhado.Domain.Entities;

namespace Compartilhado.Tests.Domain.Entities;

public class EntidadeBaseTests
{
    // Entidade concreta para poder chamar AddDomainEvent (protected)
    private class TestEntidade : EntidadeBase, IAggregateRoot
    {
        public void PublicarEvento(IDomainEvent evento) => AddDomainEvent(evento);
    }

    private record TestEvent : IDomainEvent;

    [Fact]
    public void GetDomainEvents_SemEventos_RetornaColecaoVazia()
    {
        // Arrange
        var entidade = new TestEntidade();

        // Act & Assert
        Assert.Empty(entidade.GetDomainEvents());
    }

    [Fact]
    public void AddDomainEvent_AdicionaEventoAColecao()
    {
        // Arrange
        var entidade = new TestEntidade();
        var evento = new TestEvent();

        // Act
        entidade.PublicarEvento(evento);

        // Assert
        Assert.Single(entidade.GetDomainEvents());
        Assert.Same(evento, entidade.GetDomainEvents().First());
    }

    [Fact]
    public void GetDomainEvents_RetornaReadOnly()
    {
        // Arrange
        var entidade = new TestEntidade();
        entidade.PublicarEvento(new TestEvent());

        // Act
        var eventos = entidade.GetDomainEvents();

        // Assert — IReadOnlyCollection não permite mutação direta
        Assert.IsType<IReadOnlyCollection<IDomainEvent>>(eventos, exactMatch: false);
        Assert.Throws<NotSupportedException>(() => ((IList<IDomainEvent>)eventos).Add(new TestEvent()));
    }

    [Fact]
    public void ClearDomainEvents_RemoveTodosOsEventos()
    {
        // Arrange
        var entidade = new TestEntidade();
        entidade.PublicarEvento(new TestEvent());
        entidade.PublicarEvento(new TestEvent());

        // Act
        entidade.ClearDomainEvents();

        // Assert
        Assert.Empty(entidade.GetDomainEvents());
    }

    [Fact]
    public void AddDomainEvent_MultiplasChamadas_AdicionamTodos()
    {
        // Arrange
        var entidade = new TestEntidade();

        // Act — não há deduplicação; cada chamada adiciona um evento
        entidade.PublicarEvento(new TestEvent());
        entidade.PublicarEvento(new TestEvent());
        entidade.PublicarEvento(new TestEvent());

        // Assert
        Assert.Equal(3, entidade.GetDomainEvents().Count);
    }
}
