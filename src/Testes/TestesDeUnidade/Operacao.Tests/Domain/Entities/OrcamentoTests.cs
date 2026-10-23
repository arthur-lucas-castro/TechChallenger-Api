using Compartilhado.Domain.ValueObjects;
using Operacao.Domain.Entities;
using Operacao.Domain.Excecoes;

namespace Operacao.Tests.Domain.Entities;

public class OrcamentoTests
{
    private static Orcamento CriarOrcamento(StatusOrcamento status = StatusOrcamento.Pendente) => new()
    {
        Id = 1,
        OrdemServicoId = 10,
        PrecoTotal = new Dinheiro(500m),
        Status = status,
        DataCriacao = DateTime.UtcNow
    };

    // ── Aprovar ──────────────────────────────────────────────────────────────

    [Fact]
    public void Aprovar_DePendente_AlteraParaAprovado()
    {
        // Arrange
        var orcamento = CriarOrcamento(StatusOrcamento.Pendente);

        // Act
        orcamento.Aprovar();

        // Assert
        Assert.Equal(StatusOrcamento.Aprovado, orcamento.Status);
    }

    [Fact]
    public void Aprovar_DeEnviado_AlteraParaAprovado()
    {
        // Arrange — Enviado também é pré-condição válida
        var orcamento = CriarOrcamento(StatusOrcamento.Enviado);

        // Act
        orcamento.Aprovar();

        // Assert
        Assert.Equal(StatusOrcamento.Aprovado, orcamento.Status);
    }

    [Fact]
    public void Aprovar_SetaDataAprovacao()
    {
        // Arrange
        var orcamento = CriarOrcamento(StatusOrcamento.Pendente);

        // Act
        orcamento.Aprovar();

        // Assert
        Assert.NotNull(orcamento.DataAprovacao);
    }

    [Fact]
    public void Aprovar_DeAprovado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — estado terminal
        var orcamento = CriarOrcamento(StatusOrcamento.Aprovado);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => orcamento.Aprovar());
    }

    [Fact]
    public void Aprovar_DeRecusado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — estado terminal
        var orcamento = CriarOrcamento(StatusOrcamento.Recusado);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => orcamento.Aprovar());
    }

    // ── Recusar ──────────────────────────────────────────────────────────────

    [Fact]
    public void Recusar_DePendente_AlteraParaRecusado()
    {
        // Arrange
        var orcamento = CriarOrcamento(StatusOrcamento.Pendente);

        // Act
        orcamento.Recusar();

        // Assert
        Assert.Equal(StatusOrcamento.Recusado, orcamento.Status);
    }

    [Fact]
    public void Recusar_DeEnviado_AlteraParaRecusado()
    {
        // Arrange — Enviado também é pré-condição válida
        var orcamento = CriarOrcamento(StatusOrcamento.Enviado);

        // Act
        orcamento.Recusar();

        // Assert
        Assert.Equal(StatusOrcamento.Recusado, orcamento.Status);
    }

    [Fact]
    public void Recusar_DeAprovado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — estado terminal
        var orcamento = CriarOrcamento(StatusOrcamento.Aprovado);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => orcamento.Recusar());
    }
}
