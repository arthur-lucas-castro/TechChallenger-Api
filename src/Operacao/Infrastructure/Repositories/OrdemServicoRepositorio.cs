using Operacao.Domain.Entities;
using Operacao.Domain.Interfaces;
using Compartilhado.Domain.ValueObjects;
using Compartilhado.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Operacao.Infrastructure.Repositories
{
    public class OrdemServicoRepositorio : BaseRepository<OrdemServico>, IOrdemServicoRepositorio
    {
        public OrdemServicoRepositorio(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<OrdemServico>> GetAllAsync(IReadOnlyList<StatusOrdemServico> status)
        {
            return await _context.Set<OrdemServico>()
                .Where(o => status.Contains(o.Status))
                .OrderByDescending(o => o.Status)
                .ThenBy(o => o.DataCriacao)
                .ToListAsync();
        }

        public async Task<OrdemServico?> GetByIdComServicosAsync(int id)
            => await _context.Set<OrdemServico>()
                .Include(o => o.ServicosSolicitados)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<OrdemServico?> GetByIdComPecasAsync(int id)
            => await _context.Set<OrdemServico>()
                .Include(o => o.PecasSolicitadas)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<OrdemServico?> GetByIdComItensAsync(int id)
            => await _context.Set<OrdemServico>()
                .Include(o => o.ServicosSolicitados)
                .Include(o => o.PecasSolicitadas)
                .Include(o => o.Orcamento)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<OrdemServico?> GetByIdComServicosEExecucaoAsync(int id)
            => await _context.Set<OrdemServico>()
                .Include(o => o.ServicosSolicitados)
                    .ThenInclude(s => s.ServicoExecucao)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<OrdemServico?> GetByIdDetalhadoAsync(int id)
            => await _context.Set<OrdemServico>()
                .Include(o => o.ServicosSolicitados)
                    .ThenInclude(s => s.ServicoExecucao)
                .Include(o => o.PecasSolicitadas)
                .Include(o => o.Orcamento)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<bool> CommitAsync()
            => await _context.SaveChangesAsync() > 0;

        public async Task<IEnumerable<(int ServicoId, IEnumerable<double> Tempos)>> ObterTemposExecucaoPorServicoAsync()
        {
            var lista = await _context.Set<ServicoSolicitado>()
                .Include(ss => ss.ServicoExecucao)
                .Where(ss => ss.ServicoExecucao != null
                          && ss.ServicoExecucao.Status == StatusServicoExecucao.Executado
                          && ss.ServicoExecucao.DataInicio.HasValue
                          && ss.ServicoExecucao.DataFinalizacao.HasValue)
                .ToListAsync();

            return lista
                .GroupBy(ss => ss.ServicoId)
                .Select(g => (
                    g.Key,
                    g.Select(ss => (ss.ServicoExecucao!.DataFinalizacao!.Value
                                    - ss.ServicoExecucao.DataInicio!.Value).TotalMinutes)
                ));
        }
    }
}
