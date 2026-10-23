using Atendimento.Domain.Entities;
using Atendimento.Domain.Entities.Events;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Domain.ValueObjects;

namespace Atendimento.Tests.Domain.Entities;

public class ClienteTests
{
    private static Cliente CriarCliente(int id = 1) =>
        new("João", "Silva",
            new Telefone("11987654321"),
            new Email("joao@email.com"),
            new Documento("52998224725"),
            TipoPessoa.F)
        { Id = id };

    [Fact]
    public void ResponderOrcamento_DeveAdicionarOrcamentoRespondidoEvent()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act
        cliente.ResponderOrcamento(ordemServicoId: 10, aprovado: true);

        // Assert
        Assert.Single(cliente.GetDomainEvents());
        Assert.IsType<OrcamentoRespondidoEvent>(cliente.GetDomainEvents().First());
    }

    [Fact]
    public void ResponderOrcamento_EventoDeveTerClienteIdCorreto()
    {
        // Arrange
        var cliente = CriarCliente(id: 7);

        // Act
        cliente.ResponderOrcamento(ordemServicoId: 10, aprovado: true);

        // Assert
        var evento = Assert.IsType<OrcamentoRespondidoEvent>(cliente.GetDomainEvents().First());
        Assert.Equal(7, evento.ClienteId);
    }

    [Fact]
    public void ResponderOrcamento_EventoDeveTerOrdemServicoIdCorreto()
    {
        // Arrange
        var cliente = CriarCliente();
        const int ordemServicoId = 42;

        // Act
        cliente.ResponderOrcamento(ordemServicoId, aprovado: true);

        // Assert
        var evento = Assert.IsType<OrcamentoRespondidoEvent>(cliente.GetDomainEvents().First());
        Assert.Equal(42, evento.OrdemServicoId);
    }

    [Fact]
    public void ResponderOrcamento_Aprovado_EventoDeveRefletirTrue()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act
        cliente.ResponderOrcamento(ordemServicoId: 10, aprovado: true);

        // Assert
        var evento = Assert.IsType<OrcamentoRespondidoEvent>(cliente.GetDomainEvents().First());
        Assert.True(evento.Aprovado);
    }

    [Fact]
    public void ResponderOrcamento_Recusado_EventoDeveRefletirFalse()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act
        cliente.ResponderOrcamento(ordemServicoId: 10, aprovado: false);

        // Assert
        var evento = Assert.IsType<OrcamentoRespondidoEvent>(cliente.GetDomainEvents().First());
        Assert.False(evento.Aprovado);
    }

    [Fact]
    public void ClearDomainEvents_DeveRemoverTodosOsEventos()
    {
        // Arrange
        var cliente = CriarCliente();
        cliente.ResponderOrcamento(ordemServicoId: 10, aprovado: true);

        // Act
        cliente.ClearDomainEvents();

        // Assert
        Assert.Empty(cliente.GetDomainEvents());
    }

    [Fact]
    public void GetDomainEvents_SemChamadas_DeveRetornarVazio()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act & Assert
        Assert.Empty(cliente.GetDomainEvents());
    }
}
