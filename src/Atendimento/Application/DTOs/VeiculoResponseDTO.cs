namespace Atendimento.Application.DTOs
{
    public class VeiculoResponseDto
    {
        public int Id { get; set; }
        public string Modelo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public int Ano { get; set; }
    }
}
