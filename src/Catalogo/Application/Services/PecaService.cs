using Compartilhado.Domain.Entities;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Application.Services
{
    public class PecaService : IPecaService
    {
        private readonly IPecaRepositorio _repositorio;
        private readonly IDomainEventDispatcher _dispatcher;

        public PecaService(IPecaRepositorio repositorio, IDomainEventDispatcher dispatcher)
        {
            _repositorio = repositorio;
            _dispatcher  = dispatcher;
        }

        public async Task<PecaResponseDto?> ObterPorIdAsync(int id)
        {
            var peca = await _repositorio.GetByIdAsync(id);
            return peca is null ? null : MapearParaDto(peca);
        }

        public async Task<IEnumerable<PecaResponseDto>> ObterTodosAsync()
            => (await _repositorio.GetAllAsync()).Select(MapearParaDto);

        public Task<int> CriarAsync(PecaRequestDto dto)
            => _repositorio.InsertAsync(MapearParaEntidade(dto));

        public async Task<bool> AtualizarAsync(int id, PecaRequestDto dto)
        {
            var peca = MapearParaEntidade(dto);
            peca.Id = id;
            return await _repositorio.UpdateAsync(peca);
        }

        public Task<bool> ExcluirAsync(int id)
            => _repositorio.DeleteAsync(id);

        public async Task<IEnumerable<EstoqueComPecaResponseDto>> ObterEstoqueTodosAsync()
        {
            var pecas = await _repositorio.GetAllComEstoqueAsync();
            return pecas.Select(p => new EstoqueComPecaResponseDto
            {
                PecaId           = p.Id,
                NomePeca         = p.Nome,
                DescricaoPeca    = p.Descricao,
                PrecoVendaPeca   = p.PrecoVenda,
                QuantidadeAtual  = p.ProdutoEstoque?.QuantidadeAtual,
                QuantidadeMinima = p.ProdutoEstoque?.QuantidadeMinima,
                PrecoCustoMedio  = p.ProdutoEstoque is not null ? (decimal)p.ProdutoEstoque.PrecoCustoMedio : null
            });
        }

        public async Task<EstoqueResponseDto> AdicionarEstoqueAsync(EntradaEstoqueRequestDto dto)
        {
            var peca = await _repositorio.GetByIdComEstoqueAsync(dto.PecaId)
                ?? throw new KeyNotFoundException($"Peça {dto.PecaId} não encontrada.");

            peca.AdicionarEstoque(dto.Quantidade, dto.PrecoCusto);

            if (peca.ProdutoEstoque!.Id == 0)
                await _repositorio.InsertProdutoEstoqueAsync(peca.ProdutoEstoque);
            else
                await _repositorio.UpdateProdutoEstoqueAsync(peca.ProdutoEstoque);

            return MapearEstoqueParaDto(peca.ProdutoEstoque);
        }

        public async Task<EstoqueResponseDto?> DarBaixaAsync(BaixaEstoqueRequestDto dto)
        {
            var peca = await _repositorio.GetByIdComEstoqueAsync(dto.PecaId);
            if (peca is null) return null;

            peca.DarBaixa(dto.Quantidade);
            var Estoque = peca.ProdutoEstoque!;

            await _repositorio.UpdateProdutoEstoqueAsync(Estoque);
            await _dispatcher.DispatchAsync(peca.GetDomainEvents());
            peca.ClearDomainEvents();

            return MapearEstoqueParaDto(Estoque);
        }

        private static PecaResponseDto MapearParaDto(Peca p) => new()
        {
            Id         = p.Id,
            Nome       = p.Nome,
            Descricao  = p.Descricao,
            Custo      = p.Custo,
            PrecoVenda = p.PrecoVenda
        };

        private static Peca MapearParaEntidade(PecaRequestDto dto) => new()
        {
            Nome       = dto.Nome,
            Descricao  = dto.Descricao,
            Custo      = dto.Custo,
            PrecoVenda = dto.PrecoVenda
        };

        private static EstoqueResponseDto MapearEstoqueParaDto(ProdutoEstoque e) => new()
        {
            Id               = e.Id,
            PecaId           = e.PecaId,
            QuantidadeAtual  = e.QuantidadeAtual,
            QuantidadeMinima = e.QuantidadeMinima,
            PrecoCustoMedio  = e.PrecoCustoMedio
        };
    }
}
