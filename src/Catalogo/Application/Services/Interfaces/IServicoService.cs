using Catalogo.Application.DTOs;

namespace Catalogo.Application.Services.Interfaces
{
    public interface IServicoService
    {
        Task<ServicoResponseDto?> ObterPorIdAsync(int id);
        Task<IEnumerable<ServicoResponseDto>> ObterTodosAsync();
        Task<IEnumerable<ServicoResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids);
        Task<int> CriarAsync(ServicoRequestDto dto);
        Task<bool> AtualizarAsync(int id, ServicoRequestDto dto);
        Task<bool> ExcluirAsync(int id);
    }
}
