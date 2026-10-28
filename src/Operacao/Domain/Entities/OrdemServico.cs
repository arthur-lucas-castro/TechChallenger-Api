using Operacao.Domain.Excecoes;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;

namespace Operacao.Domain.Entities
{
    public class OrdemServico : EntidadeBase, IAggregateRoot
    {
        public static readonly IReadOnlyList<StatusOrdemServico> StatusParaListarDefault =
        [
            StatusOrdemServico.EmExecucao,
            StatusOrdemServico.AguardandoAprovacao,
            StatusOrdemServico.EmDiagnostico,
            StatusOrdemServico.Recebida
        ];

        public int VeiculoId { get; set; }
        public int ClienteId { get; set; }
        public StatusOrdemServico Status { get; set; }
        public DateTime? DataUltimaAlteracao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataFinalizacao { get; set; }
        private readonly List<ServicoSolicitado> _servicosSolicitados = [];
        private readonly List<PecaSolicitada> _pecasSolicitadas = [];
        public IReadOnlyCollection<ServicoSolicitado> ServicosSolicitados => _servicosSolicitados.AsReadOnly();
        public IReadOnlyCollection<PecaSolicitada> PecasSolicitadas => _pecasSolicitadas.AsReadOnly();

        public Orcamento? Orcamento { get; private set; }

        private void TransicionarPara(StatusOrdemServico novoStatus)
        {
            var statusAnterior = Status;
            Status = novoStatus;
            DataUltimaAlteracao = DateTime.UtcNow;

            AddDomainEvent(new Events.OrdemServicoStatusAlteradoEvent(
                Id, ClienteId, statusAnterior, novoStatus, DataUltimaAlteracao.Value));
        }

        public void IniciarDiagnostico()
        {
            if (Status != StatusOrdemServico.Recebida)
                throw new TransicaoStatusInvalidaException($"Não é possível iniciar diagnóstico em uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.Recebida}'.");

            TransicionarPara(StatusOrdemServico.EmDiagnostico);
        }

        public void FinalizarDiagnostico()
        {
            if (Status != StatusOrdemServico.Recebida && Status != StatusOrdemServico.EmDiagnostico)
                throw new TransicaoStatusInvalidaException($"Não é possível finalizar diagnóstico em uma ordem com status '{Status}'. Status esperados: '{StatusOrdemServico.Recebida}' ou '{StatusOrdemServico.EmDiagnostico}'.");

            var totalServicos = ServicosSolicitados.Sum(s => s.PrecoVenda.Valor * s.Quantidade);
            var totalPecas = PecasSolicitadas.Sum(p => p.PrecoVenda.Valor * p.Quantidade);

            Orcamento = new Orcamento
            {
                OrdemServicoId = Id,
                PrecoTotal = new Dinheiro(totalServicos + totalPecas),
                Status = StatusOrcamento.Pendente,
                DataCriacao = DateTime.UtcNow
            };

            TransicionarPara(StatusOrdemServico.AguardandoAprovacao);
            AddDomainEvent(new Events.OrdemServicoDiagnosticoFinalizadoEvent(
                Id, ClienteId, VeiculoId,
                Orcamento.Id, Orcamento.PrecoTotal, Orcamento.DataCriacao));
        }

        public void IniciarExecucao()
        {
            if (Status != StatusOrdemServico.AguardandoAprovacao)
                throw new TransicaoStatusInvalidaException($"Não é possível iniciar a execução em uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.AguardandoAprovacao}'.");

            if (Orcamento is null || Orcamento.Status != StatusOrcamento.Aprovado)
                throw new OrcamentoNaoAprovadoException("O orçamento deve estar aprovado para iniciar a execução da ordem.");

            TransicionarPara(StatusOrdemServico.EmExecucao);
            AddDomainEvent(new Events.OrdemServicoIniciadaEvent(
                Id,
                PecasSolicitadas.Select(p => new Events.PecaOrdemServicoItem(p.PecaId, p.Quantidade)).ToList()
            ));
        }

        public void FinalizarOrdem()
        {
            if (Status != StatusOrdemServico.EmExecucao)
                throw new TransicaoStatusInvalidaException($"Não é possível finalizar uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.EmExecucao}'.");

            DataFinalizacao ??= DateTime.UtcNow;
            TransicionarPara(StatusOrdemServico.Finalizada);
        }

        public void EntregarVeiculo()
        {
            if (Status != StatusOrdemServico.Finalizada)
                throw new TransicaoStatusInvalidaException($"Não é possível registrar a entrega de uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.Finalizada}'.");

            DataFinalizacao ??= DateTime.UtcNow;
            TransicionarPara(StatusOrdemServico.Entregue);
        }

        public void AprovarOrcamento()
        {
            if (Status != StatusOrdemServico.AguardandoAprovacao)
                throw new TransicaoStatusInvalidaException($"Não é possível aprovar o orçamento de uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.AguardandoAprovacao}'.");

            if (Orcamento is null)
                throw new InvalidOperationException("A ordem de serviço não possui orçamento.");

            Orcamento.Aprovar();
            TransicionarPara(StatusOrdemServico.EmExecucao);
            AddDomainEvent(new Events.OrcamentoAprovadoEvent(Id, ClienteId, Orcamento.Id, Orcamento.PrecoTotal.Valor));
        }

        public void RecusarOrcamento()
        {
            if (Status != StatusOrdemServico.AguardandoAprovacao)
                throw new TransicaoStatusInvalidaException($"Não é possível recusar o orçamento de uma ordem com status '{Status}'. Status esperado: '{StatusOrdemServico.AguardandoAprovacao}'.");

            if (Orcamento is null)
                throw new InvalidOperationException("A ordem de serviço não possui orçamento.");

            Orcamento.Recusar();
            AddDomainEvent(new Events.OrcamentoRecusadoEvent(Id, ClienteId, Orcamento.Id));
        }

        public void AlterarStatusServicoExecucao(int servicoSolicitadoId, StatusServicoExecucao novoStatus)
        {
            var servicoSolicitado = ServicosSolicitados.FirstOrDefault(s => s.Id == servicoSolicitadoId)
                ?? throw new InvalidOperationException($"Serviço solicitado {servicoSolicitadoId} não encontrado na ordem.");

            servicoSolicitado.ServicoExecucao ??= new ServicoExecucao
            {
                ServicoSolicitadoId = servicoSolicitadoId,
                Status = StatusServicoExecucao.Pendente
            };

            servicoSolicitado.ServicoExecucao.Status = novoStatus;

            if (novoStatus == StatusServicoExecucao.EmExecucao)
                servicoSolicitado.ServicoExecucao.DataInicio ??= DateTime.UtcNow;

            if (novoStatus == StatusServicoExecucao.Executado)
                servicoSolicitado.ServicoExecucao.DataFinalizacao ??= DateTime.UtcNow;
        }

        public void AdicionarServico(int servicoId, int quantidade, Dinheiro precoVenda)
        {
            _servicosSolicitados.Add(new ServicoSolicitado
            {
                ServicoId = servicoId,
                Quantidade = quantidade,
                PrecoVenda = precoVenda
            });
        }

        public bool RemoverServico(int servicoId)
        {
            var servico = ServicosSolicitados.FirstOrDefault(s => s.ServicoId == servicoId);
            if (servico is null) return false;
            _servicosSolicitados.Remove(servico);
            return true;
        }

        public void AdicionarPeca(int pecaId, string nome, int quantidade, Dinheiro precoVenda)
        {
            _pecasSolicitadas.Add(new PecaSolicitada
            {
                PecaId = pecaId,
                Nome = nome,
                Quantidade = quantidade,
                PrecoVenda = precoVenda
            });
        }

        public bool RemoverPeca(int pecaId)
        {
            var peca = _pecasSolicitadas.FirstOrDefault(p => p.PecaId == pecaId);
            if (peca is null) return false;
            _pecasSolicitadas.Remove(peca);
            return true;
        }
    }
}
