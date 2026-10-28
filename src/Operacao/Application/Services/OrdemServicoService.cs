using Operacao.Application.DTOs;
using Operacao.Application.Services.Interfaces;
using Operacao.Domain.Entities;
using Operacao.Domain.Interfaces;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using StatusServicoExecucao = Compartilhado.Domain.ValueObjects.StatusServicoExecucao;
using Catalogo.Application.Services.Interfaces;
using Catalogo.Application.DTOs;
using Atendimento.Application.Services.Interfaces;
using Operacao.Domain.Excecoes;

namespace Operacao.Application.Services
{
    public class OrdemServicoService : IOrdemServicoService
    {
        private readonly IOrdemServicoRepositorio _repositorio;
        private readonly IServicoService _servicoService;
        private readonly IPecaService _pecaService;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly IClienteService _clienteService;
        private readonly IVeiculoService _veiculoService;

        public OrdemServicoService(IOrdemServicoRepositorio repositorio, IServicoService servicoService, IPecaService pecaService, IDomainEventDispatcher dispatcher, IClienteService clienteService, IVeiculoService veiculoService)
        {
            _repositorio = repositorio;
            _servicoService = servicoService;
            _pecaService = pecaService;
            _dispatcher = dispatcher;
            _clienteService = clienteService;
            _veiculoService = veiculoService;
        }

        public async Task<OrdemServicoResponseDto?> ObterPorIdAsync(int id)
        {
            var os = await _repositorio.GetByIdAsync(id);
            return os is null ? null : MapearParaDto(os);
        }

        public async Task<OrdemServicoDetalhadaResponseDto?> ObterDetalhadoPorIdAsync(int id)
        {
            var os = await _repositorio.GetByIdDetalhadoAsync(id);
            if (os is null) return null;

            var cliente = await _clienteService.ObterPorIdAsync(os.ClienteId);
            var veiculo = await _veiculoService.ObterPorIdAsync(os.VeiculoId);

            var servicoIds = os.ServicosSolicitados.Select(s => s.ServicoId).Distinct();
            var nomesServico = new Dictionary<int, string>();
            foreach (var sid in servicoIds)
            {
                var servico = await _servicoService.ObterPorIdAsync(sid);
                if (servico is not null)
                    nomesServico[sid] = servico.Nome;
            }

            return new OrdemServicoDetalhadaResponseDto
            {
                Id = os.Id,
                Status = os.Status.ToString(),
                DataCriacao = os.DataCriacao,
                DataUltimaAlteracao = os.DataUltimaAlteracao,
                DataFinalizacao = os.DataFinalizacao,

                ClienteId = os.ClienteId,
                NomeCliente = cliente?.Nome,
                SobrenomeCliente = cliente?.Sobrenome,
                TelefoneCliente = cliente?.Telefone,
                EmailCliente = cliente?.Email,
                DocumentoCliente = cliente?.NumeroDocumento,

                VeiculoId = os.VeiculoId,
                ModeloVeiculo = veiculo?.Modelo,
                MarcaVeiculo = veiculo?.Marca,
                AnoVeiculo = veiculo?.Ano,
                PlacaVeiculo = veiculo?.Placa,

                Servicos = os.ServicosSolicitados.Select(s => new ServicoSolicitadoResponseDto
                {
                    Id = s.Id,
                    ServicoId = s.ServicoId,
                    NomeServico = nomesServico.GetValueOrDefault(s.ServicoId),
                    Quantidade = s.Quantidade,
                    PrecoVenda = s.PrecoVenda.Valor,
                    StatusExecucao = s.ServicoExecucao?.Status.ToString(),
                    DataInicioExecucao = s.ServicoExecucao?.DataInicio,
                    DataFinalizacaoExecucao = s.ServicoExecucao?.DataFinalizacao
                }).ToList(),

                Pecas = os.PecasSolicitadas.Select(p => new PecaSolicitadaResponseDto
                {
                    Id = p.Id,
                    PecaId = p.PecaId,
                    Nome = p.Nome,
                    Quantidade = p.Quantidade,
                    PrecoVenda = p.PrecoVenda.Valor
                }).ToList(),

                Orcamento = os.Orcamento is null ? null : new OrcamentoResponseDto
                {
                    Id = os.Orcamento.Id,
                    PrecoTotal = os.Orcamento.PrecoTotal.Valor,
                    Status = os.Orcamento.Status.ToString(),
                    DataCriacao = os.Orcamento.DataCriacao,
                    DataEnvio = os.Orcamento.DataEnvio,
                    DataAprovacao = os.Orcamento.DataAprovacao
                }
            };
        }

        public async Task<IEnumerable<OrdemServicoResponseDto>> ObterTodosAsync(List<StatusOrdemServico>? status = null)
        {
            var statusFiltro = status is null || status.Count == 0 ? OrdemServico.StatusParaListarDefault : status;

            var ordens = (await _repositorio.GetAllAsync(statusFiltro)).ToList();

            var clienteIds = ordens.Select(o => o.ClienteId).Distinct();
            var clientes = (await _clienteService.ObterPorIdsAsync(clienteIds)).ToDictionary(c => c.Id);

            var veiculoIds = ordens.Select(o => o.VeiculoId).Distinct();
            var veiculos = (await _veiculoService.ObterPorIdsAsync(veiculoIds)).ToDictionary(v => v.Id);

            return ordens.Select(o =>
            {
                var dto = MapearParaDto(o);
                if (clientes.TryGetValue(o.ClienteId, out var c))
                {
                    dto.NomeCliente = c.Nome;
                    dto.SobrenomeCliente = c.Sobrenome;
                }
                if (veiculos.TryGetValue(o.VeiculoId, out var v))
                {
                    dto.ModeloVeiculo = v.Modelo;
                    dto.MarcaVeiculo = v.Marca;
                    dto.AnoVeiculo = v.Ano;
                    dto.PlacaVeiculo = v.Placa;
                }
                return dto;
            });
        }

        public async Task<int> CriarAsync(OrdemServicoRequestDto dto)
        {
            var ordemServico = MapearParaEntidade(dto);

            foreach (var item in dto.Servicos)
            {
                var servico = await _servicoService.ObterPorIdAsync(item.ServicoId)
                    ?? throw new KeyNotFoundException($"Serviço com Id {item.ServicoId} não encontrado.");

                ordemServico.AdicionarServico(item.ServicoId, item.Quantidade, (Dinheiro)servico.PrecoVenda);
            }

            foreach (var item in dto.Pecas)
            {
                var peca = await _pecaService.ObterPorIdAsync(item.PecaId)
                    ?? throw new KeyNotFoundException($"Peça com Id {item.PecaId} não encontrada.");

                ordemServico.AdicionarPeca(item.PecaId, peca.Nome, item.Quantidade, (Dinheiro)peca.PrecoVenda);
            }

            return await _repositorio.InsertAsync(ordemServico);
        }

        public Task<bool> ExcluirAsync(int id) => _repositorio.DeleteAsync(id);

        public async Task<IEnumerable<TempoExecucaoServicoResponseDto>> ObterTemposExecucaoPorServicoAsync()
        {
            var dados = (await _repositorio.ObterTemposExecucaoPorServicoAsync()).ToList();

            var ids = dados.Select(d => d.ServicoId);
            var nomes = (await _servicoService.ObterPorIdsAsync(ids)).ToDictionary(s => s.Id, s => s.Nome);

            return dados.Select(d => new TempoExecucaoServicoResponseDto
            {
                ServicoId           = d.ServicoId,
                NomeServico         = nomes.GetValueOrDefault(d.ServicoId, string.Empty),
                TempoMedioEmMinutos = Math.Round(d.Tempos.Average(), 2),
                PiorTempoEmMinutos  = Math.Round(d.Tempos.Max(), 2)
            });
        }

        public async Task<bool> AdicionarServicoAsync(int ordemServicoId, ServicoSolicitadoRequestDto dto)
        {
            var ordem = await _repositorio.GetByIdComServicosAsync(ordemServicoId);
            if (ordem is null) return false;

            var servico = await _servicoService.ObterPorIdAsync(dto.ServicoId)
                ?? throw new KeyNotFoundException($"Serviço com Id {dto.ServicoId} não encontrado.");

            ordem.AdicionarServico(dto.ServicoId, dto.Quantidade, (Dinheiro)servico.PrecoVenda);
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> RemoverServicoAsync(int ordemServicoId, int servicoId)
        {
            var ordem = await _repositorio.GetByIdComServicosAsync(ordemServicoId);
            if (ordem is null) return false;
            if (!ordem.RemoverServico(servicoId)) return false;
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> AdicionarPecaAsync(int ordemServicoId, PecaSolicitadaRequestDto dto)
        {
            var ordem = await _repositorio.GetByIdComPecasAsync(ordemServicoId);
            if (ordem is null) return false;

            var peca = await _pecaService.ObterPorIdAsync(dto.PecaId)
                ?? throw new KeyNotFoundException($"Peça com Id {dto.PecaId} não encontrada.");

            ordem.AdicionarPeca(dto.PecaId, peca.Nome, dto.Quantidade, (Dinheiro)peca.PrecoVenda);
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> RemoverPecaAsync(int ordemServicoId, int pecaId)
        {
            var ordem = await _repositorio.GetByIdComPecasAsync(ordemServicoId);
            if (ordem is null) return false;
            if (!ordem.RemoverPeca(pecaId)) return false;
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> IniciarServicoAsync(int ordemServicoId, int servicoSolicitadoId)
        {
            var os = await _repositorio.GetByIdComServicosEExecucaoAsync(ordemServicoId);
            if (os is null) return false;

            os.AlterarStatusServicoExecucao(servicoSolicitadoId, StatusServicoExecucao.EmExecucao);
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> FinalizarServicoAsync(int ordemServicoId, int servicoSolicitadoId)
        {
            var os = await _repositorio.GetByIdComServicosEExecucaoAsync(ordemServicoId);
            if (os is null) return false;

            os.AlterarStatusServicoExecucao(servicoSolicitadoId, StatusServicoExecucao.Executado);
            return await _repositorio.CommitAsync();
        }

        public async Task<bool> ConfirmarPagamentoAsync(int id)
        {
            var os = await _repositorio.GetByIdAsync(id);
            if (os is null) return false;

            os.EntregarVeiculo();
            var resultado = await _repositorio.CommitAsync();
            await _dispatcher.DispatchAsync(os.GetDomainEvents());
            os.ClearDomainEvents();
            return resultado;
        }

        public async Task<bool> IniciarDiagnosticoAsync(int id)
        {
            var os = await _repositorio.GetByIdAsync(id);
            if (os is null) return false;

            os.IniciarDiagnostico();
            var resultado = await _repositorio.CommitAsync();
            await _dispatcher.DispatchAsync(os.GetDomainEvents());
            os.ClearDomainEvents();
            return resultado;
        }

        public async Task<bool> FinalizarDiagnosticoAsync(int id)
        {
            var os = await _repositorio.GetByIdComItensAsync(id);
            if (os is null) return false;

            os.FinalizarDiagnostico();
            var resultado = await _repositorio.CommitAsync();
            await _dispatcher.DispatchAsync(os.GetDomainEvents());
            os.ClearDomainEvents();
            return resultado;
        }

        public async Task<bool> AlterarStatusAsync(int id, AlterarStatusOrdemServicoDto dto)
        {
            var os = await _repositorio.GetByIdComItensAsync(id);
            if (os is null) return false;

            AplicarTransicaoStatus(os, Enum.Parse<StatusOrdemServico>(dto.Status));

            var resultado = await _repositorio.CommitAsync();
            await _dispatcher.DispatchAsync(os.GetDomainEvents());
            os.ClearDomainEvents();
            return resultado;
        }

        private static void AplicarTransicaoStatus(OrdemServico os, StatusOrdemServico status)
        {
            switch (status)
            {
                case StatusOrdemServico.EmDiagnostico:
                    os.IniciarDiagnostico();
                    break;
                case StatusOrdemServico.AguardandoAprovacao:
                    os.FinalizarDiagnostico();
                    break;
                case StatusOrdemServico.EmExecucao:
                    os.IniciarExecucao();
                    break;
                case StatusOrdemServico.Finalizada:
                    os.FinalizarOrdem();
                    break;
                case StatusOrdemServico.Entregue:
                    os.EntregarVeiculo();
                    break;
                default:
                    throw new TransicaoStatusInvalidaException($"Transição para o status '{status}' não é permitida.");
            }
        }

        private static OrdemServicoResponseDto MapearParaDto(OrdemServico os) => new()
        {
            Id = os.Id,
            VeiculoId = os.VeiculoId,
            ClienteId = os.ClienteId,
            Status = os.Status.ToString(),
            DataCriacao = os.DataCriacao,
            DataUltimaAlteracao = os.DataUltimaAlteracao,
            DataFinalizacao = os.DataFinalizacao
        };

        private static OrdemServico MapearParaEntidade(OrdemServicoRequestDto dto) => new()
        {
            VeiculoId = dto.VeiculoId,
            ClienteId = dto.ClienteId,
            Status = StatusOrdemServico.Recebida,
            DataCriacao = DateTime.UtcNow
        };
    }
}
