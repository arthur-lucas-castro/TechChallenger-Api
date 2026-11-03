namespace Compartilhado.Application.Services.Interfaces
{
    /// <summary>
    /// Métricas de negócio e de falhas enviadas ao Datadog (DogStatsD), com prefixo <c>oficina.</c>.
    /// </summary>
    public interface IMetricasService
    {
        /// <summary>
        /// <c>oficina.os.abertas</c> (distribution, valor 1; consultar com <c>count:</c>): volume de ordens de
        /// serviço abertas. Distribution, e não count, porque o Datadog guarda o count do DogStatsD como taxa
        /// por segundo e nem sempre o reconverte com <c>.as_count()</c>.
        /// </summary>
        void OrdemServicoCriada();

        /// <summary><c>oficina.os.tempo_no_status</c> (distribution, segundos), tag <c>status</c>: quanto a OS ficou no status que está deixando.</summary>
        void TempoNoStatus(string status, TimeSpan duracao);

        /// <summary><c>oficina.api.falha</c> (count), tags <c>controller</c>, <c>acao</c> e <c>tipo</c>: requisições que terminaram em erro.</summary>
        void FalhaRequisicao(string controller, string acao, string tipo);

        /// <summary>
        /// <c>oficina.integracao.falha</c> (distribution, valor 1), tags <c>integracao</c> e <c>operacao</c>:
        /// falha definitiva ao falar com um sistema externo. A Lambda de autenticação envia a mesma
        /// métrica, e a biblioteca dela só envia distribution: por isso não é count.
        /// </summary>
        void FalhaIntegracao(string integracao, string operacao);

        /// <summary><c>oficina.integracao.retentativa</c> (count), tags <c>integracao</c> e <c>operacao</c>: nova tentativa após falha transitória.</summary>
        void RetentativaIntegracao(string integracao, string operacao);
    }
}
