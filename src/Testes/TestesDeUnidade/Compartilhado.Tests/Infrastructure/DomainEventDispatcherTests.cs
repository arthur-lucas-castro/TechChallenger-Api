using Compartilhado.Domain.Entities;
using Compartilhado.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Compartilhado.Tests.Infrastructure;

public class DomainEventDispatcherTests
{
    // ── Tipos auxiliares internos ────────────────────────────────────────────

    private record TestEvent : IDomainEvent;
    private record OutroEvent : IDomainEvent;

    private class TestHandler : IDomainEventHandler<TestEvent>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(TestEvent e, CancellationToken ct = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private class TestHandlerComToken : IDomainEventHandler<TestEvent>
    {
        public CancellationToken TokenRecebido { get; private set; }

        public Task HandleAsync(TestEvent e, CancellationToken ct = default)
        {
            TokenRecebido = ct;
            return Task.CompletedTask;
        }
    }

    private class OutroHandler : IDomainEventHandler<OutroEvent>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(OutroEvent e, CancellationToken ct = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private static DomainEventDispatcher CriarDispatcher(Action<IServiceCollection> registrar)
    {
        var services = new ServiceCollection();
        registrar(services);
        return new DomainEventDispatcher(services.BuildServiceProvider());
    }

    // ── Testes ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_ComUmHandler_InvocaHandlerUmaVez()
    {
        // Arrange
        var handler = new TestHandler();
        var dispatcher = CriarDispatcher(s =>
            s.AddSingleton<IDomainEventHandler<TestEvent>>(handler));

        // Act
        await dispatcher.DispatchAsync([new TestEvent()]);

        // Assert
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task DispatchAsync_ComDoisHandlers_InvocaAmbos()
    {
        // Arrange — dois handlers registrados para o mesmo evento
        var handler1 = new TestHandler();
        var handler2 = new TestHandler();
        var dispatcher = CriarDispatcher(s =>
        {
            s.AddSingleton<IDomainEventHandler<TestEvent>>(handler1);
            s.AddSingleton<IDomainEventHandler<TestEvent>>(handler2);
        });

        // Act
        await dispatcher.DispatchAsync([new TestEvent()]);

        // Assert — cada handler deve ter sido chamado exatamente uma vez
        Assert.Equal(1, handler1.CallCount);
        Assert.Equal(1, handler2.CallCount);
    }

    [Fact]
    public async Task DispatchAsync_SemHandlerRegistrado_NaoLancaExcecao()
    {
        // Arrange — nenhum handler para TestEvent
        var dispatcher = CriarDispatcher(_ => { });

        // Act & Assert — não deve lançar exceção
        var excecao = await Record.ExceptionAsync(() => dispatcher.DispatchAsync([new TestEvent()]));
        Assert.Null(excecao);
    }

    [Fact]
    public async Task DispatchAsync_ComDoisEventosDiferentes_ProcessaAmbos()
    {
        // Arrange
        var handlerTest  = new TestHandler();
        var handlerOutro = new OutroHandler();
        var dispatcher = CriarDispatcher(s =>
        {
            s.AddSingleton<IDomainEventHandler<TestEvent>>(handlerTest);
            s.AddSingleton<IDomainEventHandler<OutroEvent>>(handlerOutro);
        });

        // Act
        await dispatcher.DispatchAsync([new TestEvent(), new OutroEvent()]);

        // Assert — cada handler chamado para o seu evento específico
        Assert.Equal(1, handlerTest.CallCount);
        Assert.Equal(1, handlerOutro.CallCount);
    }

    [Fact]
    public async Task DispatchAsync_PassaCancellationToken()
    {
        // Arrange
        var handler = new TestHandlerComToken();
        var dispatcher = CriarDispatcher(s =>
            s.AddSingleton<IDomainEventHandler<TestEvent>>(handler));
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync([new TestEvent()], cts.Token);

        // Assert — o token recebido pelo handler deve ser o mesmo passado
        Assert.Equal(cts.Token, handler.TokenRecebido);
    }
}
