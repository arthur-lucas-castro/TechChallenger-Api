using Atendimento.Application.DTOs;
using Atendimento.Application.Services.Interfaces;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using Operacao.Application.DTOs;
using Operacao.Application.Services;
using Operacao.Domain.Entities;
using Operacao.Domain.Interfaces;

namespace Operacao.Tests.Application.Services;

public class OrdemServicoServiceTests
{
    private readonly Mock<IOrdemServicoRepositorio> _repositorioMock;
    private readonly Mock<IServicoService> _servicoServiceMock;
    private readonly Mock<IPecaService> _pecaServiceMock;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly Mock<IClienteService> _clienteServiceMock;
    private readonly Mock<IVeiculoService> _veiculoServiceMock;
    private readonly OrdemServicoService _service;

    public OrdemServicoServiceTests()
    {
        _repositorioMock    = new Mock<IOrdemServicoRepositorio>();
        _servicoServiceMock = new Mock<IServicoService>();
        _pecaServiceMock    = new Mock<IPecaService>();
        _dispatcherMock     = new Mock<IDomainEventDispatcher>();
        _clienteServiceMock = new Mock<IClienteService>();
        _veiculoServiceMock = new Mock<IVeiculoService>();

        _service = new OrdemServicoService(
            _repositorioMock.Object,
            _servicoServiceMock.Object,
            _pecaServiceMock.Object,
            _dispatcherMock.Object,
            _clienteServiceMock.Object,
            _veiculoServiceMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static OrdemServico CriarOrdemRecebida(int id = 1) => new()
    {
        Id = id, VeiculoId = 10, ClienteId = 20,
        Status = StatusOrdemServico.Recebida,
        DataCriacao = DateTime.UtcNow
    };

    private static OrdemServico CriarOrdemAguardandoAprovacao(int id = 1)
    {
        var os = CriarOrdemRecebida(id);
        os.FinalizarDiagnostico();
        os.ClearDomainEvents();
        return os;
    }

    private static OrdemServico CriarOrdemEmExecucao(int id = 1)
    {
        var os = CriarOrdemAguardandoAprovacao(id);
        os.Orcamento!.Aprovar();
        os.IniciarExecucao();
        os.ClearDomainEvents();
        return os;
    }

    private static OrdemServico CriarOrdemFinalizada(int id = 1)
    {
        var os = CriarOrdemEmExecucao(id);
        os.FinalizarOrdem();
        os.ClearDomainEvents();
        return os;
    }

    private void ConfigurarDispatcherOk() =>
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

    // ── ObterPorIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorIdAsync_Existe_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("Recebida", resultado.Status);
    }

    [Fact]
    public async Task ObterPorIdAsync_NaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.ObterPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    // ── ObterDetalhadoPorIdAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_OrdemNaoEncontrada_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_Existe_EnriqueceDadosDoCliente()
    {
        // Arrange
        var os = CriarOrdemRecebida(1); // ClienteId = 20
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(20))
            .ReturnsAsync(new ClienteResponseDto { Id = 20, Nome = "João", Sobrenome = "Silva", Email = "joao@email.com" });
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(10)).ReturnsAsync(new VeiculoResponseDto { Id = 10 });

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("João", resultado.NomeCliente);
        Assert.Equal("Silva", resultado.SobrenomeCliente);
        Assert.Equal("joao@email.com", resultado.EmailCliente);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_Existe_EnriqueceDadosDoVeiculo()
    {
        // Arrange
        var os = CriarOrdemRecebida(1); // VeiculoId = 10
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(20)).ReturnsAsync(new ClienteResponseDto { Id = 20 });
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(10))
            .ReturnsAsync(new VeiculoResponseDto { Id = 10, Modelo = "Gol", Marca = "Volkswagen", Ano = 2020, Placa = "ABC1234" });

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Gol", resultado.ModeloVeiculo);
        Assert.Equal("Volkswagen", resultado.MarcaVeiculo);
        Assert.Equal(2020, resultado.AnoVeiculo);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_ComServicos_MapeiaNomesServicoDoCatalogo()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        os.AdicionarServico(servicoId: 5, quantidade: 1, new Dinheiro(100m));
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(5))
            .ReturnsAsync(new ServicoResponseDto { Id = 5, Nome = "Alinhamento", PrecoVenda = 100m, TempoEstimadoEmMinutos = 30 });

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Single(resultado.Servicos);
        Assert.Equal("Alinhamento", resultado.Servicos.First().NomeServico);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_SemOrcamento_OrcamentoNuloNoDto()
    {
        // Arrange — ordem recém-criada não tem orçamento
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Null(resultado.Orcamento);
    }

    // ── ObterTodosAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ObterTodosAsync_EnriqueceDtosComNomeClienteEDadosVeiculo()
    {
        // Arrange
        var ordens = new List<OrdemServico> { CriarOrdemRecebida(1) }; // ClienteId=20, VeiculoId=10
        _repositorioMock.Setup(r => r.GetAllAsync(OrdemServico.StatusParaListarDefault)).ReturnsAsync(ordens);
        _clienteServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ClienteResponseDto { Id = 20, Nome = "Maria", Sobrenome = "Santos" }]);
        _veiculoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new VeiculoResponseDto { Id = 10, Modelo = "Civic", Marca = "Honda", Ano = 2022, Placa = "XYZ9A87" }]);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Maria", resultado[0].NomeCliente);
        Assert.Equal("Civic", resultado[0].ModeloVeiculo);
        Assert.Equal("Honda", resultado[0].MarcaVeiculo);
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoClienteNaoEncontrado_MantemCamposNulos()
    {
        // Arrange — cliente não encontrado (lista vazia)
        var ordens = new List<OrdemServico> { CriarOrdemRecebida(1) };
        _repositorioMock.Setup(r => r.GetAllAsync(OrdemServico.StatusParaListarDefault)).ReturnsAsync(ordens);
        _clienteServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);
        _veiculoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert — campos de cliente/veículo ficam nulos pois TryGetValue não encontra
        Assert.Single(resultado);
        Assert.Null(resultado[0].NomeCliente);
        Assert.Null(resultado[0].ModeloVeiculo);
    }

    [Fact]
    public async Task ObterTodosAsync_ComStatus_RepassaStatusAoRepositorio()
    {
        // Arrange
        var statusFiltro = new List<StatusOrdemServico> { StatusOrdemServico.EmExecucao, StatusOrdemServico.Recebida };
        _repositorioMock.Setup(r => r.GetAllAsync(statusFiltro)).ReturnsAsync([]);
        _clienteServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        _veiculoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);

        // Act
        await _service.ObterTodosAsync(statusFiltro);

        // Assert
        _repositorioMock.Verify(r => r.GetAllAsync(statusFiltro), Times.Once);
    }

    // ── ObterTemposExecucaoPorServicoAsync ───────────────────────────────────

    [Fact]
    public async Task ObterTemposExecucaoPorServicoAsync_CalculaTempoMedioCorretamente()
    {
        // Arrange
        // tempos = [30.0, 45.0, 60.0] → média = 45.0
        _repositorioMock.Setup(r => r.ObterTemposExecucaoPorServicoAsync())
            .ReturnsAsync([(ServicoId: 1, Tempos: (IEnumerable<double>)[30.0, 45.0, 60.0])]);
        _servicoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ServicoResponseDto { Id = 1, Nome = "Alinhamento" }]);

        // Act
        var resultado = (await _service.ObterTemposExecucaoPorServicoAsync()).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal(45.0, resultado[0].TempoMedioEmMinutos);
    }

    [Fact]
    public async Task ObterTemposExecucaoPorServicoAsync_CalculaPiorTempoCorretamente()
    {
        // Arrange
        // tempos = [30.0, 45.0, 90.0] → pior = 90.0
        _repositorioMock.Setup(r => r.ObterTemposExecucaoPorServicoAsync())
            .ReturnsAsync([(ServicoId: 1, Tempos: (IEnumerable<double>)[30.0, 45.0, 90.0])]);
        _servicoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ServicoResponseDto { Id = 1, Nome = "Revisão" }]);

        // Act
        var resultado = (await _service.ObterTemposExecucaoPorServicoAsync()).ToList();

        // Assert
        Assert.Equal(90.0, resultado[0].PiorTempoEmMinutos);
    }

    [Fact]
    public async Task ObterTemposExecucaoPorServicoAsync_ArredondaTempoMedioParaDuasCasas()
    {
        // Arrange
        // tempos = [10.0, 10.0, 11.0] → média = 10.333... → arredondado = 10.33
        _repositorioMock.Setup(r => r.ObterTemposExecucaoPorServicoAsync())
            .ReturnsAsync([(ServicoId: 2, Tempos: (IEnumerable<double>)[10.0, 10.0, 11.0])]);
        _servicoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ServicoResponseDto { Id = 2, Nome = "Balanceamento" }]);

        // Act
        var resultado = (await _service.ObterTemposExecucaoPorServicoAsync()).ToList();

        // Assert — Math.Round(..., 2) aplica arredondamento bancário por padrão
        Assert.Equal(Math.Round(10.333333, 2), resultado[0].TempoMedioEmMinutos);
    }

    [Fact]
    public async Task ObterTemposExecucaoPorServicoAsync_BuscaNomeServicoNoCatalogo()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObterTemposExecucaoPorServicoAsync())
            .ReturnsAsync([(ServicoId: 7, Tempos: (IEnumerable<double>)[60.0])]);
        _servicoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ServicoResponseDto { Id = 7, Nome = "Troca de Óleo" }]);

        // Act
        var resultado = (await _service.ObterTemposExecucaoPorServicoAsync()).ToList();

        // Assert
        Assert.Equal("Troca de Óleo", resultado[0].NomeServico);
    }

    [Fact]
    public async Task ObterTemposExecucaoPorServicoAsync_ServicoNaoEncontradoNoCatalogo_UsaNomeVazio()
    {
        // Arrange — catálogo não encontra o serviço (lista vazia)
        _repositorioMock.Setup(r => r.ObterTemposExecucaoPorServicoAsync())
            .ReturnsAsync([(ServicoId: 99, Tempos: (IEnumerable<double>)[30.0])]);
        _servicoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        var resultado = (await _service.ObterTemposExecucaoPorServicoAsync()).ToList();

        // Assert — GetValueOrDefault retorna string.Empty quando serviço não existe
        Assert.Equal(string.Empty, resultado[0].NomeServico);
    }

    // ── CriarAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CriarAsync_ServicoNaoEncontrado_LancaKeyNotFoundException()
    {
        // Arrange
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(99)).ReturnsAsync((ServicoResponseDto?)null);
        var dto = new OrdemServicoRequestDto
        {
            VeiculoId = 10, ClienteId = 20,
            Servicos = [new ServicoSolicitadoRequestDto { ServicoId = 99, Quantidade = 1 }],
            Pecas = []
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CriarAsync(dto));
    }

    [Fact]
    public async Task CriarAsync_PecaNaoEncontrada_LancaKeyNotFoundException()
    {
        // Arrange
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(1))
            .ReturnsAsync(new ServicoResponseDto { Id = 1, PrecoVenda = 100m, Nome = "Alinhamento", TempoEstimadoEmMinutos = 30 });
        _pecaServiceMock.Setup(p => p.ObterPorIdAsync(99)).ReturnsAsync((PecaResponseDto?)null);
        var dto = new OrdemServicoRequestDto
        {
            VeiculoId = 10, ClienteId = 20,
            Servicos = [new ServicoSolicitadoRequestDto { ServicoId = 1, Quantidade = 1 }],
            Pecas = [new PecaSolicitadaRequestDto { PecaId = 99, Quantidade = 1 }]
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CriarAsync(dto));
    }

    [Fact]
    public async Task CriarAsync_Sucesso_ChamaInsertAsync()
    {
        // Arrange
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(1))
            .ReturnsAsync(new ServicoResponseDto { Id = 1, PrecoVenda = 100m, Nome = "Alinhamento", TempoEstimadoEmMinutos = 30 });
        _repositorioMock.Setup(r => r.InsertAsync(It.IsAny<OrdemServico>())).ReturnsAsync(5);
        var dto = new OrdemServicoRequestDto
        {
            VeiculoId = 10, ClienteId = 20,
            Servicos = [new ServicoSolicitadoRequestDto { ServicoId = 1, Quantidade = 2 }],
            Pecas = []
        };

        // Act
        var id = await _service.CriarAsync(dto);

        // Assert
        Assert.Equal(5, id);
        _repositorioMock.Verify(r => r.InsertAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_StatusInicialEhRecebida()
    {
        // Arrange
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(1))
            .ReturnsAsync(new ServicoResponseDto { Id = 1, PrecoVenda = 80m, Nome = "Revisão", TempoEstimadoEmMinutos = 60 });
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<OrdemServico>(os => os.Status == StatusOrdemServico.Recebida)))
            .ReturnsAsync(1);
        var dto = new OrdemServicoRequestDto
        {
            VeiculoId = 10, ClienteId = 20,
            Servicos = [new ServicoSolicitadoRequestDto { ServicoId = 1, Quantidade = 1 }],
            Pecas = []
        };

        // Act
        await _service.CriarAsync(dto);

        // Assert — entidade inserida deve ter status Recebida
        _repositorioMock.Verify(
            r => r.InsertAsync(It.Is<OrdemServico>(os => os.Status == StatusOrdemServico.Recebida)),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComPecas_AdicionaPecaComNomeEPrecoCorretos()
    {
        // Arrange
        _pecaServiceMock.Setup(p => p.ObterPorIdAsync(3))
            .ReturnsAsync(new PecaResponseDto { Id = 3, Nome = "Filtro de Óleo", PrecoVenda = 45m });
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<OrdemServico>(os =>
                os.PecasSolicitadas.Any(p => p.Nome == "Filtro de Óleo" && (decimal)p.PrecoVenda == 45m))))
            .ReturnsAsync(1);
        var dto = new OrdemServicoRequestDto
        {
            VeiculoId = 10, ClienteId = 20,
            Servicos = [],
            Pecas = [new PecaSolicitadaRequestDto { PecaId = 3, Quantidade = 2 }]
        };

        // Act
        await _service.CriarAsync(dto);

        // Assert — peça adicionada com nome e preço vindos do catálogo
        _repositorioMock.Verify(
            r => r.InsertAsync(It.Is<OrdemServico>(os =>
                os.PecasSolicitadas.Any(p => p.Nome == "Filtro de Óleo"))),
            Times.Once);
    }

    // ── ExcluirAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExcluirAsync_DelegaAoRepositorio()
    {
        // Arrange
        _repositorioMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var resultado = await _service.ExcluirAsync(1);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    // ── IniciarDiagnosticoAsync ──────────────────────────────────────────────

    [Fact]
    public async Task IniciarDiagnosticoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.IniciarDiagnosticoAsync(99);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task IniciarDiagnosticoAsync_Sucesso_ChamaCommit()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        var resultado = await _service.IniciarDiagnosticoAsync(1);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── FinalizarDiagnosticoAsync ────────────────────────────────────────────

    [Fact]
    public async Task FinalizarDiagnosticoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.FinalizarDiagnosticoAsync(99);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task FinalizarDiagnosticoAsync_Sucesso_ChamaCommitEDispatch()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.FinalizarDiagnosticoAsync(1);

        // Assert — commit ANTES do dispatch
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizarDiagnosticoAsync_Sucesso_LimpaEventosAposDispatch()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.FinalizarDiagnosticoAsync(1);

        // Assert
        Assert.Empty(os.GetDomainEvents());
    }

    // ── AlterarStatusAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AlterarStatusAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.AlterarStatusAsync(99, new AlterarStatusOrdemServicoDto { Status = "EmDiagnostico" });

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task AlterarStatusAsync_TransicaoParaEmDiagnostico_AlteraStatusDaOrdem()
    {
        // Arrange — Recebida → EmDiagnostico
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "EmDiagnostico" });

        // Assert — a transição foi aplicada à entidade
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_TransicaoParaEntregue_AlteraStatusDaOrdem()
    {
        // Arrange — Finalizada → Entregue
        var os = CriarOrdemFinalizada(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "Entregue" });

        // Assert
        Assert.Equal(StatusOrdemServico.Entregue, os.Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_Sucesso_ChamaCommitEDispatch()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "EmDiagnostico" });

        // Assert
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── AdicionarServicoAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AdicionarServicoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.AdicionarServicoAsync(99, new ServicoSolicitadoRequestDto { ServicoId = 1, Quantidade = 1 });

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task AdicionarServicoAsync_ServicoNaoEncontrado_LancaKeyNotFoundException()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(99)).ReturnsAsync((ServicoResponseDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AdicionarServicoAsync(1, new ServicoSolicitadoRequestDto { ServicoId = 99, Quantidade = 1 }));
    }

    [Fact]
    public async Task AdicionarServicoAsync_Sucesso_ChamaCommit()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(1))
            .ReturnsAsync(new ServicoResponseDto { Id = 1, PrecoVenda = 100m, Nome = "Revisão", TempoEstimadoEmMinutos = 30 });
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.AdicionarServicoAsync(1, new ServicoSolicitadoRequestDto { ServicoId = 1, Quantidade = 2 });

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── RemoverServicoAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RemoverServicoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.RemoverServicoAsync(99, 1);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task RemoverServicoAsync_ServicoNaoNaOrdem_RetornaFalse()
    {
        // Arrange — ordem sem o serviço com servicoId=99
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));

        // Act
        var resultado = await _service.RemoverServicoAsync(1, servicoId: 99);

        // Assert
        Assert.False(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoverServicoAsync_Sucesso_ChamaCommit()
    {
        // Arrange — adiciona serviço e depois remove
        var os = CriarOrdemRecebida(1);
        os.AdicionarServico(servicoId: 3, 1, new Dinheiro(100m));
        _repositorioMock.Setup(r => r.GetByIdComServicosAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.RemoverServicoAsync(1, servicoId: 3);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── AdicionarPecaAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AdicionarPecaAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.AdicionarPecaAsync(99, new PecaSolicitadaRequestDto { PecaId = 1, Quantidade = 1 });

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task AdicionarPecaAsync_PecaNaoEncontrada_LancaKeyNotFoundException()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));
        _pecaServiceMock.Setup(p => p.ObterPorIdAsync(99)).ReturnsAsync((PecaResponseDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AdicionarPecaAsync(1, new PecaSolicitadaRequestDto { PecaId = 99, Quantidade = 1 }));
    }

    [Fact]
    public async Task AdicionarPecaAsync_Sucesso_ChamaCommit()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));
        _pecaServiceMock.Setup(p => p.ObterPorIdAsync(1))
            .ReturnsAsync(new PecaResponseDto { Id = 1, Nome = "Filtro de Óleo", PrecoVenda = 45m });
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.AdicionarPecaAsync(1, new PecaSolicitadaRequestDto { PecaId = 1, Quantidade = 2 });

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── RemoverPecaAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RemoverPecaAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.RemoverPecaAsync(99, pecaId: 1);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task RemoverPecaAsync_PecaNaoNaOrdem_RetornaFalse()
    {
        // Arrange — ordem sem peças
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(1)).ReturnsAsync(CriarOrdemRecebida(1));

        // Act
        var resultado = await _service.RemoverPecaAsync(1, pecaId: 99);

        // Assert
        Assert.False(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoverPecaAsync_Sucesso_ChamaCommit()
    {
        // Arrange — adiciona peça e depois remove
        var os = CriarOrdemRecebida(1);
        os.AdicionarPeca(pecaId: 5, "Filtro de Óleo", 1, new Dinheiro(45m));
        _repositorioMock.Setup(r => r.GetByIdComPecasAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.RemoverPecaAsync(1, pecaId: 5);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── IniciarServicoAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task IniciarServicoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosEExecucaoAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.IniciarServicoAsync(99, servicoSolicitadoId: 1);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task IniciarServicoAsync_Sucesso_ChamaCommit()
    {
        // Arrange — adiciona serviço (Id padrão = 0) e inicia com esse Id
        var os = CriarOrdemRecebida(1);
        os.AdicionarServico(servicoId: 1, 1, new Dinheiro(100m));
        var servicoSolicitadoId = os.ServicosSolicitados.First().Id; // 0
        _repositorioMock.Setup(r => r.GetByIdComServicosEExecucaoAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.IniciarServicoAsync(1, servicoSolicitadoId);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── FinalizarServicoAsync ────────────────────────────────────────────────

    [Fact]
    public async Task FinalizarServicoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdComServicosEExecucaoAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.FinalizarServicoAsync(99, servicoSolicitadoId: 1);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task FinalizarServicoAsync_Sucesso_ChamaCommit()
    {
        // Arrange
        var os = CriarOrdemRecebida(1);
        os.AdicionarServico(servicoId: 1, 1, new Dinheiro(100m));
        var servicoSolicitadoId = os.ServicosSolicitados.First().Id; // 0
        _repositorioMock.Setup(r => r.GetByIdComServicosEExecucaoAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);

        // Act
        var resultado = await _service.FinalizarServicoAsync(1, servicoSolicitadoId);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
    }

    // ── ConfirmarPagamentoAsync ──────────────────────────────────────────────

    [Fact]
    public async Task ConfirmarPagamentoAsync_OrdemNaoEncontrada_RetornaFalse()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((OrdemServico?)null);

        // Act
        var resultado = await _service.ConfirmarPagamentoAsync(99);

        // Assert
        Assert.False(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmarPagamentoAsync_OrdemFinalizada_AlteraStatusParaEntregue()
    {
        // Arrange — Finalizada → Entregue
        var os = CriarOrdemFinalizada(1);
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.ConfirmarPagamentoAsync(1);

        // Assert
        Assert.Equal(StatusOrdemServico.Entregue, os.Status);
    }

    [Fact]
    public async Task ConfirmarPagamentoAsync_Sucesso_ChamaCommit()
    {
        // Arrange
        var os = CriarOrdemFinalizada(1);
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        var resultado = await _service.ConfirmarPagamentoAsync(1);

        // Assert
        Assert.True(resultado);
        _repositorioMock.Verify(r => r.CommitAsync(), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmarPagamentoAsync_OrdemNaoFinalizada_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — ordem em Recebida não pode ir para Entregue diretamente
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<Operacao.Domain.Excecoes.TransicaoStatusInvalidaException>(() =>
            _service.ConfirmarPagamentoAsync(1));
    }

    // ── AplicarTransicaoStatus — branches faltantes ──────────────────────────

    [Fact]
    public async Task AlterarStatusAsync_TransicaoParaAguardandoAprovacao_FinalizaDiagnostico()
    {
        // Arrange — Recebida → AguardandoAprovacao via FinalizarDiagnostico()
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "AguardandoAprovacao" });

        // Assert
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_TransicaoParaEmExecucao_IniciaNaExecucao()
    {
        // Arrange — AguardandoAprovacao + orçamento aprovado → EmExecucao via IniciarExecucao()
        var os = CriarOrdemAguardandoAprovacao(1);
        os.Orcamento!.Aprovar();   // aprova direto sem passar por AprovarOrcamento()
        os.ClearDomainEvents();
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "EmExecucao" });

        // Assert
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_TransicaoParaFinalizada_FinalizaOrdem()
    {
        // Arrange — EmExecucao → Finalizada via FinalizarOrdem()
        var os = CriarOrdemEmExecucao(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);
        _repositorioMock.Setup(r => r.CommitAsync()).ReturnsAsync(true);
        ConfigurarDispatcherOk();

        // Act
        await _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "Finalizada" });

        // Assert
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
    }

    [Fact]
    public async Task AlterarStatusAsync_StatusInvalido_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — "Recebida" é um enum válido mas não está no switch → default → exception
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdComItensAsync(1)).ReturnsAsync(os);

        // Act & Assert
        await Assert.ThrowsAsync<Operacao.Domain.Excecoes.TransicaoStatusInvalidaException>(() =>
            _service.AlterarStatusAsync(1, new AlterarStatusOrdemServicoDto { Status = "Recebida" }));
    }

    // ── ObterDetalhadoPorIdAsync — cliente/veiculo nulos e pecas ────────────

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_ClienteNaoEncontrado_CamposClienteNulos()
    {
        // Arrange — cliente service retorna null; nullable access cliente?.Nome deve resultar em null
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync((ClienteResponseDto?)null);
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Null(resultado.NomeCliente);
        Assert.Null(resultado.SobrenomeCliente);
        Assert.Null(resultado.EmailCliente);
        Assert.Null(resultado.TelefoneCliente);
        Assert.Null(resultado.DocumentoCliente);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_VeiculoNaoEncontrado_CamposVeiculoNulos()
    {
        // Arrange — veiculo service retorna null; nullable access veiculo?.Modelo deve resultar em null
        var os = CriarOrdemRecebida(1);
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync((VeiculoResponseDto?)null);

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Null(resultado.ModeloVeiculo);
        Assert.Null(resultado.MarcaVeiculo);
        Assert.Null(resultado.AnoVeiculo);
        Assert.Null(resultado.PlacaVeiculo);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_ComPecas_MapeiaListaDePecas()
    {
        // Arrange — ordem com peça adicionada; branch PecasSolicitadas.Select(...)
        var os = CriarOrdemRecebida(1);
        os.AdicionarPeca(pecaId: 7, "Filtro de Óleo", quantidade: 2, new Dinheiro(45m));
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Single(resultado.Pecas);
        Assert.Equal(7, resultado.Pecas[0].PecaId);
        Assert.Equal("Filtro de Óleo", resultado.Pecas[0].Nome);
        Assert.Equal(2, resultado.Pecas[0].Quantidade);
        Assert.Equal(45m, resultado.Pecas[0].PrecoVenda);
    }

    // ── ObterTodosAsync — branches independentes de cliente e veiculo ────────

    [Fact]
    public async Task ObterTodosAsync_SoClienteEncontrado_MantemCamposVeiculoNulos()
    {
        // Arrange — TryGetValue para cliente retorna true, para veiculo retorna false
        var ordens = new List<OrdemServico> { CriarOrdemRecebida(1) };
        _repositorioMock.Setup(r => r.GetAllAsync(OrdemServico.StatusParaListarDefault)).ReturnsAsync(ordens);
        _clienteServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new ClienteResponseDto { Id = 20, Nome = "Ana", Sobrenome = "Lima" }]);
        _veiculoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Ana", resultado[0].NomeCliente);
        Assert.Null(resultado[0].ModeloVeiculo);
        Assert.Null(resultado[0].MarcaVeiculo);
    }

    [Fact]
    public async Task ObterTodosAsync_SoVeiculoEncontrado_MantemCamposClienteNulos()
    {
        // Arrange — TryGetValue para veiculo retorna true, para cliente retorna false
        var ordens = new List<OrdemServico> { CriarOrdemRecebida(1) };
        _repositorioMock.Setup(r => r.GetAllAsync(OrdemServico.StatusParaListarDefault)).ReturnsAsync(ordens);
        _clienteServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);
        _veiculoServiceMock.Setup(s => s.ObterPorIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([new VeiculoResponseDto { Id = 10, Modelo = "Onix", Marca = "Chevrolet" }]);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Null(resultado[0].NomeCliente);
        Assert.Null(resultado[0].SobrenomeCliente);
        Assert.Equal("Onix", resultado[0].ModeloVeiculo);
        Assert.Equal("Chevrolet", resultado[0].MarcaVeiculo);
    }

    // ── ObterDetalhadoPorIdAsync — branches faltantes ────────────────────────

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_ComOrcamento_MapeiaDadosDoOrcamento()
    {
        // Arrange — ordem em AguardandoAprovacao já possui Orcamento (criado por FinalizarDiagnostico)
        var os = CriarOrdemAguardandoAprovacao(1);
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert — branch `os.Orcamento is null ? null : new OrcamentoResponseDto` → não-nulo
        Assert.NotNull(resultado);
        Assert.NotNull(resultado.Orcamento);
        Assert.Equal("Pendente", resultado.Orcamento.Status);
    }

    [Fact]
    public async Task ObterDetalhadoPorIdAsync_ServicoNaoEncontradoNoCatalogo_NaoAdicionaNomeAoDicionario()
    {
        // Arrange — order com serviço, mas catálogo retorna null → branch `if (servico is not null)` = false
        var os = CriarOrdemRecebida(1);
        os.AdicionarServico(servicoId: 7, quantidade: 1, new Dinheiro(80m));
        _repositorioMock.Setup(r => r.GetByIdDetalhadoAsync(1)).ReturnsAsync(os);
        _clienteServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new ClienteResponseDto());
        _veiculoServiceMock.Setup(s => s.ObterPorIdAsync(It.IsAny<int>())).ReturnsAsync(new VeiculoResponseDto());
        _servicoServiceMock.Setup(s => s.ObterPorIdAsync(7)).ReturnsAsync((ServicoResponseDto?)null);

        // Act
        var resultado = await _service.ObterDetalhadoPorIdAsync(1);

        // Assert — nome fica null pois não foi adicionado ao dicionário
        Assert.NotNull(resultado);
        Assert.Single(resultado.Servicos);
        Assert.Null(resultado.Servicos.First().NomeServico);
    }
}
