namespace Atendimento.Application.DTOs
{
    public class VeiculoRequestDto
    {
        public string Modelo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public int Ano { get; set; }
    }
}
