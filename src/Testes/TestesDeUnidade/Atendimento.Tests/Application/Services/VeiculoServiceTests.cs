using Atendimento.Application.DTOs;
using Atendimento.Application.Services;
using Atendimento.Domain.Entities;
using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using System.Linq.Expressions;

namespace Atendimento.Tests.Application.Services;

public class VeiculoServiceTests
{
    private readonly Mock<IVeiculoRepositorio> _repositorioMock;
    private readonly VeiculoService _service;

    public VeiculoServiceTests()
    {
        _repositorioMock = new Mock<IVeiculoRepositorio>();
        _service = new VeiculoService(_repositorioMock.Object);
    }

    private static Veiculo CriarVeiculo(int id = 1) => new()
    {
        Id = id, Modelo = "Gol", Placa = new Placa("ABC1234"), Marca = "Volkswagen", Ano = 2020
    };

    // --- Queries ---

    [Fact]
    public async Task ObterPorIdAsync_VeiculoExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CriarVeiculo(1));

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("Gol", resultado.Modelo);
        Assert.Equal("ABC1234", resultado.Placa);
        Assert.Equal("Volkswagen", resultado.Marca);
        Assert.Equal(2020, resultado.Ano);
    }

    [Fact]
    public async Task ObterPorIdAsync_VeiculoNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Veiculo?)null);

        // Act
        var resultado = await _service.ObterPorIdAsync(99);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterTodosAsync_RetornaTodosOsVeiculosMapeados()
    {
        // Arrange
        var veiculos = new List<Veiculo> { CriarVeiculo(1), CriarVeiculo(2) };
        _repositorioMock.Setup(r => r.GetAllAsync()).ReturnsAsync(veiculos);

        // Act
        var resultado = (await _service.ObterTodosAsync()).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ObterPorIdsAsync_FiltrarPorIds_RetornaSubconjunto()
    {
        // Arrange
        var veiculos = new List<Veiculo> { CriarVeiculo(1) };
        _repositorioMock
            .Setup(r => r.GetByExpressionAsync(It.IsAny<Expression<Func<Veiculo, bool>>>()))
            .ReturnsAsync(veiculos);

        // Act
        var resultado = (await _service.ObterPorIdsAsync([1])).ToList();

        // Assert
        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
    }

    [Fact]
    public async Task ObterPorPlacaAsync_PlacaExiste_RetornaDto()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByPlacaAsync("ABC1234")).ReturnsAsync(CriarVeiculo(1));

        // Act
        var resultado = await _service.ObterPorPlacaAsync("ABC1234");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("ABC1234", resultado.Placa);
        Assert.Equal("Gol", resultado.Modelo);
    }

    [Fact]
    public async Task ObterPorPlacaAsync_PlacaNaoExiste_RetornaNull()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByPlacaAsync(It.IsAny<string>())).ReturnsAsync((Veiculo?)null);

        // Act
        var resultado = await _service.ObterPorPlacaAsync("ZZZ9999");

        // Assert
        Assert.Null(resultado);
    }

    // --- Commands ---

    [Fact]
    public async Task CriarAsync_DeveChamarInsertComEntidadeCorreta()
    {
        // Arrange
        var dto = new VeiculoRequestDto
        {
            Modelo = "Civic", Placa = "XYZ9A87", Marca = "Honda", Ano = 2023
        };
        _repositorioMock
            .Setup(r => r.InsertAsync(It.Is<Veiculo>(v => v.Modelo == "Civic" && v.Marca == "Honda")))
            .ReturnsAsync(3);

        // Act
        var id = await _service.CriarAsync(dto);

        // Assert
        Assert.Equal(3, id);
        _repositorioMock.Verify(r => r.InsertAsync(It.Is<Veiculo>(v => v.Modelo == "Civic")), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_VeiculoExiste_RetornaTrue()
    {
        // Arrange
        var dto = new VeiculoRequestDto
        {
            Modelo = "Gol", Placa = "ABC1234", Marca = "Volkswagen", Ano = 2021
        };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Veiculo>())).ReturnsAsync(true);

        // Act
        var resultado = await _service.AtualizarAsync(1, dto);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task AtualizarAsync_VeiculoNaoExiste_RetornaFalse()
    {
        // Arrange
        var dto = new VeiculoRequestDto
        {
            Modelo = "Gol", Placa = "ABC1234", Marca = "Volkswagen", Ano = 2021
        };
        _repositorioMock.Setup(r => r.UpdateAsync(It.IsAny<Veiculo>())).ReturnsAsync(false);

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
