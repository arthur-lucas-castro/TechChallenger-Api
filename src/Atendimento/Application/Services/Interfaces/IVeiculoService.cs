using Atendimento.Application.DTOs;

namespace Atendimento.Application.Services.Interfaces
{
    public interface IVeiculoService
    {
        Task<VeiculoResponseDto?> ObterPorIdAsync(int id);
        Task<IEnumerable<VeiculoResponseDto>> ObterTodosAsync();
        Task<IEnumerable<VeiculoResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids);
        Task<int> CriarAsync(VeiculoRequestDto dto);
        Task<bool> AtualizarAsync(int id, VeiculoRequestDto dto);
        Task<bool> ExcluirAsync(int id);
        Task<VeiculoResponseDto?> ObterPorPlacaAsync(string placa);
    }
}
