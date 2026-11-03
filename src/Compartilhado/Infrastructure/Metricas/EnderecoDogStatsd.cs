namespace Compartilhado.Infrastructure.Metricas
{
    /// <summary>
    /// Endereço do DogStatsD do agente Datadog, a partir das variáveis que o admission controller
    /// injeta no pod. O chart atual usa socket unix (DD_DOGSTATSD_URL=unix:///var/run/datadog/dsd.socket);
    /// no modo hostip, injeta DD_AGENT_HOST. O cliente lê DD_AGENT_HOST sozinho, mas não DD_DOGSTATSD_URL.
    /// </summary>
    public static class EnderecoDogStatsd
    {
        /// <returns>Servidor no formato do StatsdConfig (host ou unix://caminho) e porta UDP, se houver; servidor nulo sem agente.</returns>
        public static (string? Servidor, int? Porta) Resolver(string? dogstatsdUrl, string? agentHost)
        {
            if (!string.IsNullOrWhiteSpace(dogstatsdUrl))
            {
                if (dogstatsdUrl.StartsWith("unix://", StringComparison.OrdinalIgnoreCase))
                    return (dogstatsdUrl, null);

                if (dogstatsdUrl.StartsWith("udp://", StringComparison.OrdinalIgnoreCase)
                    && Uri.TryCreate(dogstatsdUrl, UriKind.Absolute, out var uri))
                    return (uri.Host, uri.IsDefaultPort ? null : uri.Port);
            }

            return string.IsNullOrWhiteSpace(agentHost) ? (null, null) : (agentHost, null);
        }
    }
}
