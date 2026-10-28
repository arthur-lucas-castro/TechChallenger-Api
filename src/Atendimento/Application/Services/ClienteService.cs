using Atendimento.Application.DTOs;
using Atendimento.Application.Services.Interfaces;
using Atendimento.Domain.Interfaces;
using Compartilhado.Domain.Entities;
using ClienteEntity = Atendimento.Domain.Entities.Cliente;

namespace Atendimento.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepositorio _repositorio;
        private readonly IDomainEventDispatcher _dispatcher;

        public ClienteService(IClienteRepositorio repositorio, IDomainEventDispatcher dispatcher)
        {
            _repositorio = repositorio;
            _dispatcher = dispatcher;
        }

        public async Task<ClienteResponseDto?> ObterPorIdAsync(int id)
        {
            var c = await _repositorio.GetByIdAsync(id);
            return c is null ? null : MapearParaDto(c);
        }

        public async Task<IEnumerable<ClienteResponseDto>> ObterTodosAsync()
            => (await _repositorio.GetAllAsync()).Select(MapearParaDto);

        public async Task<IEnumerable<ClienteResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids)
            => (await _repositorio.GetByExpressionAsync(c => ids.Contains(c.Id))).Select(MapearParaDto);

        public Task<int> CriarAsync(ClienteRequestDto dto)
            => _repositorio.InsertAsync(CriarEntidade(dto));

        public async Task<bool> AtualizarAsync(int id, ClienteRequestDto dto)
        {
            var e = CriarEntidade(dto);
            e.Id = id;
            return await _repositorio.UpdateAsync(e);
        }

        public Task<bool> ExcluirAsync(int id) => _repositorio.DeleteAsync(id);

        public async Task<ClienteResponseDto?> ObterPorNumeroDocumentoAsync(string numeroDocumento)
        {
            var c = await _repositorio.ObterPorNumeroDocumentoAsync(numeroDocumento);
            return c is null ? null : MapearParaDto(c);
        }

        public async Task<bool> ResponderOrcamentoAsync(int clienteId, int ordemServicoId, ResponderOrcamentoDto dto)
        {
            var cliente = await _repositorio.GetByIdAsync(clienteId);
            if (cliente is null) return false;

            cliente.ResponderOrcamento(ordemServicoId, dto.Aprovado);
            await _dispatcher.DispatchAsync(cliente.GetDomainEvents());
            cliente.ClearDomainEvents();
            return true;
        }

        private static ClienteResponseDto MapearParaDto(ClienteEntity c) => new()
        {
            Id = c.Id, Nome = c.Nome, Sobrenome = c.Sobrenome,
            Telefone = c.Telefone, Email = c.Email,
            NumeroDocumento = c.NumeroDocumento, TipoPessoa = c.TipoPessoa
        };

        private static ClienteEntity CriarEntidade(ClienteRequestDto dto) => new()
        {
            Nome = dto.Nome, Sobrenome = dto.Sobrenome,
            Telefone = dto.Telefone, Email = dto.Email,
            NumeroDocumento = dto.NumeroDocumento, TipoPessoa = dto.TipoPessoa
        };
    }
}
