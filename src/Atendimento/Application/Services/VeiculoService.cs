using Atendimento.Application.DTOs;
using Atendimento.Application.Services.Interfaces;
using Atendimento.Domain.Entities;
using Atendimento.Domain.Interfaces;

namespace Atendimento.Application.Services
{
    public class VeiculoService : IVeiculoService
    {
        private readonly IVeiculoRepositorio _repositorio;

        public VeiculoService(IVeiculoRepositorio repositorio) => _repositorio = repositorio;

        public async Task<VeiculoResponseDto?> ObterPorIdAsync(int id)
        {
            var v = await _repositorio.GetByIdAsync(id);
            return v is null ? null : MapearParaDto(v);
        }

        public async Task<IEnumerable<VeiculoResponseDto>> ObterTodosAsync()
            => (await _repositorio.GetAllAsync()).Select(MapearParaDto);

        public async Task<IEnumerable<VeiculoResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids)
            => (await _repositorio.GetByExpressionAsync(v => ids.Contains(v.Id))).Select(MapearParaDto);

        public Task<int> CriarAsync(VeiculoRequestDto dto)
            => _repositorio.InsertAsync(MapearParaEntidade(dto));

        public async Task<bool> AtualizarAsync(int id, VeiculoRequestDto dto)
        {
            var e = MapearParaEntidade(dto);
            e.Id = id;
            return await _repositorio.UpdateAsync(e);
        }

        public Task<bool> ExcluirAsync(int id) => _repositorio.DeleteAsync(id);

        public async Task<VeiculoResponseDto?> ObterPorPlacaAsync(string placa)
        {
            var v = await _repositorio.GetByPlacaAsync(placa);
            return v is null ? null : MapearParaDto(v);
        }

        private static VeiculoResponseDto MapearParaDto(Veiculo v) => new()
        {
            Id = v.Id, Modelo = v.Modelo, Placa = v.Placa, Marca = v.Marca, Ano = v.Ano
        };

        private static Veiculo MapearParaEntidade(VeiculoRequestDto dto) => new()
        {
            Modelo = dto.Modelo, Placa = dto.Placa, Marca = dto.Marca, Ano = dto.Ano
        };
    }
}
