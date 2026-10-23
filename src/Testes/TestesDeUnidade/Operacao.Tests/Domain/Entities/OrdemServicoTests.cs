using Compartilhado.Domain.ValueObjects;
using Operacao.Domain.Entities;
using Operacao.Domain.Entities.Events;
using Operacao.Domain.Excecoes;

namespace Operacao.Tests.Domain.Entities;

public class OrdemServicoTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static OrdemServico CriarOrdem(StatusOrdemServico status = StatusOrdemServico.Recebida) => new()
    {
        Id = 1, VeiculoId = 10, ClienteId = 20,
        Status = status,
        DataCriacao = DateTime.UtcNow
    };

    private static OrdemServico CriarOrdemEmDiagnostico()
    {
        var os = CriarOrdem();
        os.IniciarDiagnostico();
        os.ClearDomainEvents();
        return os;
    }

    private static OrdemServico CriarOrdemAguardandoAprovacao()
    {
        var os = CriarOrdem();
        os.FinalizarDiagnostico();
        os.ClearDomainEvents();
        return os;
    }

    private static OrdemServico CriarOrdemEmExecucao()
    {
        var os = CriarOrdemAguardandoAprovacao();
        os.Orcamento!.Aprovar();  // aprova direto, sem passar por AprovarOrcamento()
        os.IniciarExecucao();
        os.ClearDomainEvents();
        return os;
    }

    private static OrdemServico CriarOrdemFinalizada()
    {
        var os = CriarOrdemEmExecucao();
        os.FinalizarOrdem();
        os.ClearDomainEvents();
        return os;
    }

    // ── IniciarDiagnostico ───────────────────────────────────────────────────

    [Fact]
    public void IniciarDiagnostico_StatusRecebida_AlteraParaEmDiagnostico()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act
        os.IniciarDiagnostico();

        // Assert
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
    }

    [Fact]
    public void IniciarDiagnostico_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — qualquer status diferente de Recebida
        var os = CriarOrdem(StatusOrdemServico.EmDiagnostico);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.IniciarDiagnostico());
    }

    [Fact]
    public void IniciarDiagnostico_PublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act
        os.IniciarDiagnostico();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.Recebida, evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, evento.NovoStatus);
    }

    // ── FinalizarDiagnostico ─────────────────────────────────────────────────

    [Fact]
    public void FinalizarDiagnostico_DeRecebida_AlteraParaAguardandoAprovacao()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act
        os.FinalizarDiagnostico();

        // Assert
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void FinalizarDiagnostico_DeEmDiagnostico_AlteraParaAguardandoAprovacao()
    {
        // Arrange
        var os = CriarOrdemEmDiagnostico();

        // Act
        os.FinalizarDiagnostico();

        // Assert
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void FinalizarDiagnostico_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — EmExecucao não é válido para finalizar diagnóstico
        var os = CriarOrdemEmExecucao();

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.FinalizarDiagnostico());
    }

    [Fact]
    public void FinalizarDiagnostico_CriaOrcamentoComPrecoTotalCorreto()
    {
        // Arrange
        // 1 serviço: PrecoVenda=100 × Quantidade=2 = 200
        // 1 peça:    PrecoVenda=50  × Quantidade=3 = 150
        // PrecoTotal esperado = 350
        var os = CriarOrdem();
        os.AdicionarServico(1, 2, new Dinheiro(100m));
        os.AdicionarPeca(1, "Filtro de Óleo", 3, new Dinheiro(50m));

        // Act
        os.FinalizarDiagnostico();

        // Assert
        Assert.Equal(350m, (decimal)os.Orcamento!.PrecoTotal);
    }

    [Fact]
    public void FinalizarDiagnostico_OrcamentoCriadoComStatusPendente()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        os.FinalizarDiagnostico();

        // Assert
        Assert.NotNull(os.Orcamento);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamento.Status);
    }

    [Fact]
    public void FinalizarDiagnostico_PublicaOrdemServicoDiagnosticoFinalizadoEvent()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        os.FinalizarDiagnostico();

        // Assert
        Assert.Contains(os.GetDomainEvents(), e => e is OrdemServicoDiagnosticoFinalizadoEvent);
    }

    [Fact]
    public void FinalizarDiagnostico_PublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act
        os.FinalizarDiagnostico();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.Recebida, evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, evento.NovoStatus);
    }

    // ── IniciarExecucao ──────────────────────────────────────────────────────

    [Fact]
    public void IniciarExecucao_OrcamentoAprovado_AlteraParaEmExecucao()
    {
        // Arrange — orçamento aprovado diretamente, sem passar por AprovarOrcamento()
        var os = CriarOrdemAguardandoAprovacao();
        os.Orcamento!.Aprovar();

        // Act
        os.IniciarExecucao();

        // Assert
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
    }

    [Fact]
    public void IniciarExecucao_OrcamentoAprovado_PublicaOrdemServicoIniciadaEvent()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();
        os.Orcamento!.Aprovar();
        os.ClearDomainEvents();

        // Act
        os.IniciarExecucao();

        // Assert
        Assert.Contains(os.GetDomainEvents(), e => e is OrdemServicoIniciadaEvent);
    }

    [Fact]
    public void IniciarExecucao_OrcamentoAprovado_EventoTemPecasCorretas()
    {
        // Arrange — adiciona peças antes de fechar o diagnóstico
        var os = CriarOrdem();
        os.AdicionarPeca(5, "Filtro", 2, new Dinheiro(50m));
        os.AdicionarPeca(8, "Vela", 4, new Dinheiro(10m));
        os.FinalizarDiagnostico();
        os.Orcamento!.Aprovar();
        os.ClearDomainEvents();

        // Act
        os.IniciarExecucao();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoIniciadaEvent>().Single();
        Assert.Equal(2, evento.Pecas.Count);
        Assert.Contains(evento.Pecas, p => p.PecaId == 5 && p.Quantidade == 2);
        Assert.Contains(evento.Pecas, p => p.PecaId == 8 && p.Quantidade == 4);
    }

    [Fact]
    public void IniciarExecucao_OrcamentoAprovado_PublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();
        os.Orcamento!.Aprovar();
        os.ClearDomainEvents();

        // Act
        os.IniciarExecucao();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.EmExecucao, evento.NovoStatus);
    }

    [Fact]
    public void IniciarExecucao_OrcamentoNulo_LancaOrcamentoNaoAprovadoException()
    {
        // Arrange — força status AguardandoAprovacao sem orçamento (via object initializer)
        var os = new OrdemServico
        {
            Id = 1, VeiculoId = 10, ClienteId = 20,
            Status = StatusOrdemServico.AguardandoAprovacao,
            DataCriacao = DateTime.UtcNow
        };

        // Act & Assert
        Assert.Throws<OrcamentoNaoAprovadoException>(() => os.IniciarExecucao());
    }

    [Fact]
    public void IniciarExecucao_OrcamentoPendente_LancaOrcamentoNaoAprovadoException()
    {
        // Arrange — orçamento existe mas está Pendente (não aprovado)
        var os = CriarOrdemAguardandoAprovacao(); // Orcamento.Status == Pendente

        // Act & Assert
        Assert.Throws<OrcamentoNaoAprovadoException>(() => os.IniciarExecucao());
    }

    [Fact]
    public void IniciarExecucao_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange — status diferente de AguardandoAprovacao
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.IniciarExecucao());
    }

    // ── FinalizarOrdem ───────────────────────────────────────────────────────

    [Fact]
    public void FinalizarOrdem_StatusEmExecucao_AlteraParaFinalizada()
    {
        // Arrange
        var os = CriarOrdemEmExecucao();

        // Act
        os.FinalizarOrdem();

        // Assert
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
    }

    [Fact]
    public void FinalizarOrdem_SetaDataFinalizacao()
    {
        // Arrange
        var os = CriarOrdemEmExecucao();

        // Act
        os.FinalizarOrdem();

        // Assert
        Assert.NotNull(os.DataFinalizacao);
    }

    [Fact]
    public void FinalizarOrdem_PublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange
        var os = CriarOrdemEmExecucao();

        // Act
        os.FinalizarOrdem();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.EmExecucao, evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.Finalizada, evento.NovoStatus);
    }

    [Fact]
    public void FinalizarOrdem_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.FinalizarOrdem());
    }

    // ── EntregarVeiculo ──────────────────────────────────────────────────────

    [Fact]
    public void EntregarVeiculo_StatusFinalizada_AlteraParaEntregue()
    {
        // Arrange
        var os = CriarOrdemFinalizada();

        // Act
        os.EntregarVeiculo();

        // Assert
        Assert.Equal(StatusOrdemServico.Entregue, os.Status);
    }

    [Fact]
    public void EntregarVeiculo_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.EmExecucao);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.EntregarVeiculo());
    }

    [Fact]
    public void EntregarVeiculo_PublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange
        var os = CriarOrdemFinalizada();

        // Act
        os.EntregarVeiculo();

        // Assert
        var evento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.Finalizada, evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.Entregue, evento.NovoStatus);
    }

    // ── AprovarOrcamento ─────────────────────────────────────────────────────

    [Fact]
    public void AprovarOrcamento_AlteraStatusParaEmExecucao()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();

        // Act
        os.AprovarOrcamento();

        // Assert
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
    }

    [Fact]
    public void AprovarOrcamento_PublicaOrcamentoAprovadoEvent()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();
        os.ClearDomainEvents();

        // Act
        os.AprovarOrcamento();

        // Assert
        Assert.Contains(os.GetDomainEvents(), e => e is OrcamentoAprovadoEvent);
    }

    [Fact]
    public void AprovarOrcamento_PublicaOrcamentoAprovadoEvent_EOrdemServicoStatusAlteradoEvent()
    {
        // Arrange — garante que a transição via AprovarOrcamento não perde nem duplica eventos
        var os = CriarOrdemAguardandoAprovacao();
        os.ClearDomainEvents();

        // Act
        os.AprovarOrcamento();

        // Assert
        Assert.Single(os.GetDomainEvents().OfType<OrcamentoAprovadoEvent>());
        var statusEvento = os.GetDomainEvents().OfType<OrdemServicoStatusAlteradoEvent>().Single();
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, statusEvento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.EmExecucao, statusEvento.NovoStatus);
    }

    [Fact]
    public void AprovarOrcamento_SemOrcamento_LancaInvalidOperationException()
    {
        // Arrange — AguardandoAprovacao sem orçamento
        var os = new OrdemServico
        {
            Id = 1, VeiculoId = 10, ClienteId = 20,
            Status = StatusOrdemServico.AguardandoAprovacao,
            DataCriacao = DateTime.UtcNow
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => os.AprovarOrcamento());
    }

    [Fact]
    public void AprovarOrcamento_StatusErrado_LancaTransicaoStatusInvalidaException()
    {
        // Arrange
        var os = CriarOrdem(StatusOrdemServico.Recebida);

        // Act & Assert
        Assert.Throws<TransicaoStatusInvalidaException>(() => os.AprovarOrcamento());
    }

    // ── RecusarOrcamento ─────────────────────────────────────────────────────

    [Fact]
    public void RecusarOrcamento_NaoAlteraStatusDaOrdem()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();

        // Act
        os.RecusarOrcamento();

        // Assert — status da ORDEM permanece AguardandoAprovacao (diferente de AprovarOrcamento)
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void RecusarOrcamento_PublicaOrcamentoRecusadoEvent()
    {
        // Arrange
        var os = CriarOrdemAguardandoAprovacao();
        os.ClearDomainEvents();

        // Act
        os.RecusarOrcamento();

        // Assert
        Assert.Contains(os.GetDomainEvents(), e => e is OrcamentoRecusadoEvent);
    }

    [Fact]
    public void RecusarOrcamento_NaoPublicaOrdemServicoStatusAlteradoEvent()
    {
        // Arrange — status da OS não muda ao recusar, então TransicionarPara não é chamado
        var os = CriarOrdemAguardandoAprovacao();
        os.ClearDomainEvents();

        // Act
        os.RecusarOrcamento();

        // Assert
        Assert.DoesNotContain(os.GetDomainEvents(), e => e is OrdemServicoStatusAlteradoEvent);
    }

    [Fact]
    public void RecusarOrcamento_SemOrcamento_LancaInvalidOperationException()
    {
        // Arrange
        var os = new OrdemServico
        {
            Id = 1, VeiculoId = 10, ClienteId = 20,
            Status = StatusOrdemServico.AguardandoAprovacao,
            DataCriacao = DateTime.UtcNow
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => os.RecusarOrcamento());
    }

    // ── Itens ────────────────────────────────────────────────────────────────

    [Fact]
    public void AdicionarServico_AdicionaAColecao()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        os.AdicionarServico(1, 2, new Dinheiro(100m));

        // Assert
        Assert.Single(os.ServicosSolicitados);
        Assert.Equal(1, os.ServicosSolicitados.First().ServicoId);
    }

    [Fact]
    public void RemoverServico_Existente_RetornaTrue()
    {
        // Arrange
        var os = CriarOrdem();
        os.AdicionarServico(servicoId: 1, 2, new Dinheiro(100m));

        // Act
        var resultado = os.RemoverServico(servicoId: 1);

        // Assert
        Assert.True(resultado);
        Assert.Empty(os.ServicosSolicitados);
    }

    [Fact]
    public void RemoverServico_Inexistente_RetornaFalse()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        var resultado = os.RemoverServico(servicoId: 99);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void AdicionarPeca_AdicionaAColecao()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        os.AdicionarPeca(1, "Filtro", 3, new Dinheiro(50m));

        // Assert
        Assert.Single(os.PecasSolicitadas);
        Assert.Equal(1, os.PecasSolicitadas.First().PecaId);
    }

    [Fact]
    public void RemoverPeca_Existente_RetornaTrue()
    {
        // Arrange
        var os = CriarOrdem();
        os.AdicionarPeca(pecaId: 5, "Filtro", 1, new Dinheiro(50m));

        // Act
        var resultado = os.RemoverPeca(pecaId: 5);

        // Assert
        Assert.True(resultado);
        Assert.Empty(os.PecasSolicitadas);
    }

    [Fact]
    public void RemoverPeca_Inexistente_RetornaFalse()
    {
        // Arrange
        var os = CriarOrdem();

        // Act
        var resultado = os.RemoverPeca(pecaId: 99);

        // Assert
        Assert.False(resultado);
    }

    // ── AlterarStatusServicoExecucao ─────────────────────────────────────────

    [Fact]
    public void AlterarStatusServicoExecucao_CriaServicoExecucaoSeNulo()
    {
        // Arrange — adiciona serviço (Id padrão = 0)
        var os = CriarOrdem();
        os.AdicionarServico(1, 1, new Dinheiro(100m));
        var servicoSolicitadoId = os.ServicosSolicitados.First().Id; // 0

        // Act
        os.AlterarStatusServicoExecucao(servicoSolicitadoId, StatusServicoExecucao.Pendente);

        // Assert — ServicoExecucao criado implicitamente
        Assert.NotNull(os.ServicosSolicitados.First().ServicoExecucao);
    }

    [Fact]
    public void AlterarStatusServicoExecucao_EmExecucao_SetaDataInicio()
    {
        // Arrange
        var os = CriarOrdem();
        os.AdicionarServico(1, 1, new Dinheiro(100m));
        var servicoSolicitadoId = os.ServicosSolicitados.First().Id;

        // Act
        os.AlterarStatusServicoExecucao(servicoSolicitadoId, StatusServicoExecucao.EmExecucao);

        // Assert
        Assert.NotNull(os.ServicosSolicitados.First().ServicoExecucao!.DataInicio);
    }

    [Fact]
    public void AlterarStatusServicoExecucao_Executado_SetaDataFinalizacao()
    {
        // Arrange
        var os = CriarOrdem();
        os.AdicionarServico(1, 1, new Dinheiro(100m));
        var servicoSolicitadoId = os.ServicosSolicitados.First().Id;

        // Act
        os.AlterarStatusServicoExecucao(servicoSolicitadoId, StatusServicoExecucao.Executado);

        // Assert
        Assert.NotNull(os.ServicosSolicitados.First().ServicoExecucao!.DataFinalizacao);
    }

    [Fact]
    public void AlterarStatusServicoExecucao_ServicoNaoEncontrado_LancaInvalidOperationException()
    {
        // Arrange — ordem sem serviços
        var os = CriarOrdem();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            os.AlterarStatusServicoExecucao(servicoSolicitadoId: 99, StatusServicoExecucao.EmExecucao));
    }
}
