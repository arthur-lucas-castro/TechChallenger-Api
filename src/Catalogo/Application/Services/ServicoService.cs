using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;

namespace Catalogo.Application.Services
{
    public class ServicoService : IServicoService
    {
        private readonly IServicoRepositorio _repositorio;

        public ServicoService(IServicoRepositorio repositorio) => _repositorio = repositorio;

        public async Task<ServicoResponseDto?> ObterPorIdAsync(int id)
        {
            var s = await _repositorio.GetByIdAsync(id);
            return s is null ? null : MapearParaDto(s);
        }

        public async Task<IEnumerable<ServicoResponseDto>> ObterTodosAsync()
            => (await _repositorio.GetAllAsync()).Select(MapearParaDto);

        public Task<int> CriarAsync(ServicoRequestDto dto)
            => _repositorio.InsertAsync(MapearParaEntidade(dto));

        public async Task<bool> AtualizarAsync(int id, ServicoRequestDto dto)
        {
            var e = MapearParaEntidade(dto);
            e.Id = id;
            return await _repositorio.UpdateAsync(e);
        }

        public Task<bool> ExcluirAsync(int id) => _repositorio.DeleteAsync(id);

        public async Task<IEnumerable<ServicoResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.ToList();
            var servicos = await _repositorio.GetByExpressionAsync(s => idList.Contains(s.Id));
            return servicos.Select(MapearParaDto);
        }

        private static ServicoResponseDto MapearParaDto(Servico s) => new()
        {
            Id = s.Id, Nome = s.Nome, PrecoVenda = s.PrecoVenda,
            TempoEstimadoEmMinutos = s.TempoEstimadoEmMinutos
        };

        private static Servico MapearParaEntidade(ServicoRequestDto dto) => new()
        {
            Nome = dto.Nome, PrecoVenda = dto.PrecoVenda,
            TempoEstimadoEmMinutos = dto.TempoEstimadoEmMinutos
        };
    }
}
