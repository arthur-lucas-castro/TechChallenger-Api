using Catalogo.Application.DTOs;
using Catalogo.Application.Services;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;
using Compartilhado.Domain.ValueObjects;
using System.Linq.Expressions;

namespace Catalogo.Tests.Application.Services;

public class ServicoServiceTests
{
    private readonly Mock<IServicoRepositorio> _repositorioMock;
    private readonly ServicoService _service;

    public ServicoServiceTests()
    {
        _repositorioMock = new Mock<IServicoRepositorio>();
        _service = new ServicoService(_repositorioMock.Object);
    }

    private static Servico CriarServico(int id = 1) => new()
    {
        Id = id,
        Nome = "Troca de Óleo",
        PrecoVenda = new Dinheiro(80.00m),
        TempoEstimadoEmMinutos = 30
    };

    [Fact]
    public async Task ObterPorIdAsync_ServicoExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarServico(1));

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("Troca de Óleo", resultado.Nome);
        Assert.Equal(80.00m, resultado.PrecoVenda);
        Assert.Equal(30, resultado.TempoEstimadoEmMinutos);
    }

    [Fact]
    public async Task ObterPorIdAsync_ServicoNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Servico?)null);

        // Act
        var resultado = await _service.ObterPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterTodosAsync_RetornaTodosMapeados()
    {
        // Arrange
        var servicos = new List<Servico> { CriarServico(1), CriarServico(2) };
        _repositorioMock.Setup(r => r.GetAllAsync()).ReturnsAsync(servicos);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ObterPorIdsAsync_RetornaSubconjunto()
    {
        // Arrange
        var servicos = new List<Servico> { CriarServico(1) };
        _repositorioMock
            .Setup(r => r.GetByExpressionAsync(It.IsAny<Expression<Func<Servico, bool>>>()))
            .ReturnsAsync(servicos);

        // Act
        var resultado = (await _service.ObterPorIdsAsync([1])).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
    }

    [Fact]
    public async Task CriarAsync_ChamaInsertComEntidadeCorreta()
    {
        // Arrange
        var dto = new ServicoRequestDto
        {
            Nome = "Alinhamento", PrecoVenda = 120.00m, TempoEstimadoEmMinutos = 60
        };
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<Servico>(s => s.Nome == "Alinhamento")))
            .ReturnsAsync(3);

        // Act
        var id = await _service.CriarAsync(dto);

        // Assert
        Assert.Equal(3, id);
        _repositorioMock.Verify(r => r.InsertAsync(It.Is<Servico>(s => s.Nome == "Alinhamento")), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_ServicoExiste_RetornaTrue()
    {
        // Arrange
        var dto = new ServicoRequestDto { Nome = "Troca de Óleo", PrecoVenda = 90m, TempoEstimadoEmMinutos = 30 };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Servico>())).ReturnsAsync(true);

        // Act
        var resultado = await _service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task AtualizarAsync_ServicoNaoExiste_RetornaFalse()
    {
        // Arrange
        var dto = new ServicoRequestDto { Nome = "Troca de Óleo", PrecoVenda = 90m, TempoEstimadoEmMinutos = 30 };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Servico>())).ReturnsAsync(false);

        // Act
        var resultado = await _service.AtualizarAsync(99, dto);

        // Assert
        Assert.False(resultado);
    }

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
}
