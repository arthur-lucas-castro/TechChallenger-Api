using Catalogo.Application.DTOs;

namespace Catalogo.Application.Services.Interfaces
{
    public interface IPecaService
    {
        Task<PecaResponseDto?> ObterPorIdAsync(int id);
        Task<IEnumerable<PecaResponseDto>> ObterTodosAsync();
        Task<int> CriarAsync(PecaRequestDto dto);
        Task<bool> AtualizarAsync(int id, PecaRequestDto dto);
        Task<bool> ExcluirAsync(int id);

        Task<IEnumerable<EstoqueComPecaResponseDto>> ObterEstoqueTodosAsync();
        Task<EstoqueResponseDto> AdicionarEstoqueAsync(EntradaEstoqueRequestDto dto);
        Task<EstoqueResponseDto?> DarBaixaAsync(BaixaEstoqueRequestDto dto);
    }
}
