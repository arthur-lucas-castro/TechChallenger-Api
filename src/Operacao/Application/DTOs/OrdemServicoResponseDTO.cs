namespace Operacao.Application.DTOs
{
    public class OrdemServicoResponseDto
    {
        public int Id { get; set; }
        public int VeiculoId { get; set; }
        public string? ModeloVeiculo { get; set; }
        public string? MarcaVeiculo { get; set; }
        public int? AnoVeiculo { get; set; }
        public string? PlacaVeiculo { get; set; }
        public int ClienteId { get; set; }
        public string? NomeCliente { get; set; }
        public string? SobrenomeCliente { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataUltimaAlteracao { get; set; }
        public DateTime? DataFinalizacao { get; set; }
    }

    public class OrdemServicoDetalhadaResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataUltimaAlteracao { get; set; }
        public DateTime? DataFinalizacao { get; set; }

        public int ClienteId { get; set; }
        public string? NomeCliente { get; set; }
        public string? SobrenomeCliente { get; set; }
        public string? TelefoneCliente { get; set; }
        public string? EmailCliente { get; set; }
        public string? DocumentoCliente { get; set; }

        public int VeiculoId { get; set; }
        public string? ModeloVeiculo { get; set; }
        public string? MarcaVeiculo { get; set; }
        public int? AnoVeiculo { get; set; }
        public string? PlacaVeiculo { get; set; }

        public List<ServicoSolicitadoResponseDto> Servicos { get; set; } = [];
        public List<PecaSolicitadaResponseDto> Pecas { get; set; } = [];
        public OrcamentoResponseDto? Orcamento { get; set; }
    }

    public class ServicoSolicitadoResponseDto
    {
        public int Id { get; set; }
        public int ServicoId { get; set; }
        public string? NomeServico { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoVenda { get; set; }
        public string? StatusExecucao { get; set; }
        public DateTime? DataInicioExecucao { get; set; }
        public DateTime? DataFinalizacaoExecucao { get; set; }
    }

    public class PecaSolicitadaResponseDto
    {
        public int Id { get; set; }
        public int PecaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoVenda { get; set; }
    }

    public class OrcamentoResponseDto
    {
        public int Id { get; set; }
        public decimal PrecoTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataEnvio { get; set; }
        public DateTime? DataAprovacao { get; set; }
    }
}
