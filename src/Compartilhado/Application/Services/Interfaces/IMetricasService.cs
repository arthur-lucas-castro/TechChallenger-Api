namespace Compartilhado.Application.Services.Interfaces
{
    /// <summary>
    /// Métricas de negócio e de falhas enviadas ao Datadog (DogStatsD), com prefixo <c>oficina.</c>.
    /// </summary>
    public interface IMetricasService
    {
        /// <summary><c>oficina.os.criada</c> (count): volume de ordens de serviço abertas.</summary>
        void OrdemServicoCriada();

        /// <summary><c>oficina.os.tempo_no_status</c> (distribution, segundos), tag <c>status</c>: quanto a OS ficou no status que está deixando.</summary>
        void TempoNoStatus(string status, TimeSpan duracao);

        /// <summary><c>oficina.api.falha</c> (count), tags <c>controller</c>, <c>acao</c> e <c>tipo</c>: requisições que terminaram em erro.</summary>
        void FalhaRequisicao(string controller, string acao, string tipo);
    }
}
