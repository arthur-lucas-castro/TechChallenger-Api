using Microsoft.EntityFrameworkCore;
using Atendimento.Domain.Entities;
using Atendimento.Domain.ValueObjects;
using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;
using Operacao.Domain.Entities;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;

namespace Compartilhado.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class AppDbContext : DbContext
    {
        private const string NumericDecimal = "numeric(10,2)";

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Atendimento.Domain.Entities.Cliente> Cliente { get; set; }
        public DbSet<Veiculo> Veiculo { get; set; }
        public DbSet<Servico> Servico { get; set; }
        public DbSet<Peca> Peca { get; set; }
        public DbSet<OrdemServico> OrdemServico { get; set; }
        public DbSet<ServicoSolicitado> ServicoSolicitado { get; set; }
        public DbSet<PecaSolicitada> PecaSolicitada { get; set; }
        public DbSet<ProdutoEstoque> Estoque { get; set; }
        public DbSet<Orcamento> Orcamento { get; set; }
        public DbSet<ServicoExecucao> ServicoExecucao { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<StatusOrdemServico>("status_ordem_servico");

            modelBuilder.Entity<Usuario>(b =>
            {
                b.Property(x => x.Email).HasMaxLength(100);
                b.Property(x => x.SenhaHash).HasMaxLength(72);
                b.Property(x => x.Tipo)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                b.HasIndex(x => x.Email).IsUnique();
            });
            modelBuilder.Entity<Atendimento.Domain.Entities.Cliente>(b =>
            {
                b.Property(x => x.Telefone)
                    .HasConversion(v => v.Valor, v => new Telefone(v));
                b.Property(x => x.Email)
                    .HasConversion(v => v.Valor, v => new Email(v));
                b.Property(x => x.NumeroDocumento)
                    .HasConversion(v => v.Numero, v => new Documento(v));
                b.Property(x => x.TipoPessoa)
                    .HasConversion<string>()
                    .HasMaxLength(1);
            });

            modelBuilder.Entity<Veiculo>(b =>
            {
                b.Property(x => x.Placa)
                    .HasConversion(v => v.Valor, v => new Placa(v))
                    .HasMaxLength(7);
            });

            modelBuilder.Entity<Servico>(b =>
            {
                b.Property(x => x.PrecoVenda)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
            });

            modelBuilder.Entity<Peca>(b =>
            {
                b.Property(x => x.Custo)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
                b.Property(x => x.PrecoVenda)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
                b.HasOne(x => x.ProdutoEstoque)
                    .WithOne()
                    .HasForeignKey<ProdutoEstoque>(e => e.PecaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrdemServico>(b =>
            {
                b.HasMany(x => x.ServicosSolicitados)
                    .WithOne()
                    .HasForeignKey(s => s.OrdemServicoId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.PecasSolicitadas)
                    .WithOne()
                    .HasForeignKey(p => p.OrdemServicoId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.Orcamento)
                    .WithOne()
                    .HasForeignKey<Orcamento>(o => o.OrdemServicoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Orcamento>(b =>
            {
                b.Property(x => x.PrecoTotal)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
                b.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<ServicoSolicitado>(b =>
            {
                b.Property(x => x.PrecoVenda)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
                b.HasOne(x => x.ServicoExecucao)
                    .WithOne()
                    .HasForeignKey<ServicoExecucao>(e => e.ServicoSolicitadoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ServicoExecucao>(b =>
            {
                b.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<PecaSolicitada>(b =>
            {
                b.Property(x => x.PrecoVenda)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
            });


            modelBuilder.Entity<ProdutoEstoque>(b =>
            {
                b.ToTable("produtoestoque");
                b.Property(x => x.PrecoCustoMedio)
                    .HasConversion(v => v.Valor, v => new Dinheiro(v))
                    .HasColumnType(NumericDecimal);
            });
        }
    }
}
