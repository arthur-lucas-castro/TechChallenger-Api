using System.Text;
using Compartilhado.Infrastructure.Repositories;
using Compartilhado.Domain.Entities;
using Compartilhado.Domain.Entities.Interfaces;
using Compartilhado.Application.Services;
using Compartilhado.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Atendimento.Domain.Interfaces;
using Atendimento.Application.Services;
using Atendimento.Application.Services.Interfaces;
using Atendimento.Infrastructure.Repositories;
using Catalogo.Domain.Interfaces;
using Catalogo.Domain.Entities.Events;
using Catalogo.Application.Services;
using Catalogo.Application.Services.Interfaces;
using Catalogo.Application.Services.Events;
using Catalogo.Infrastructure.Repositories;
using Operacao.Domain.Interfaces;
using Operacao.Application.Services;
using Operacao.Application.Services.Interfaces;
using Operacao.Application.Services.Events;
using Atendimento.Application.Services.Events;
using Operacao.Domain.Entities.Events;
using Atendimento.Domain.Entities.Events;
using Operacao.Infrastructure.Repositories;
using Npgsql;
using Compartilhado.Domain.ValueObjects;
using Compartilhado.Infrastructure.Email;
using Compartilhado.Presentation.Middlewares;
using Compartilhado.Infrastructure.Metricas;
using StatsdClient;
using Serilog;
using Serilog.Events;
using Serilog.Templates;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

string[] swaggerXmlFiles =
[
    "Compartilhado.Presentation.xml",
    "Atendimento.Presentation.Controllers.xml",
    "Catalogo.Presentation.Controllers.xml",
    "Operacao.Presentation.Controllers.xml",
];

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Uma linha JSON por evento, lida pelo agente Datadog. "level" e "message" são
// atributos reservados do Datadog; rest() traz as demais propriedades, como o
// CorrelationId e os dd.trace_id/dd.span_id injetados pelo tracer (DD_LOGS_INJECTION).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new ExpressionTemplate(
        "{ {timestamp: @t, level: @l, message: @m, exception: @x, logger: SourceContext, ..rest()} }\n")));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TechChallenger API", Version = "v1" });
    options.SwaggerDoc("GestaoAdministrativa", new OpenApiInfo { Title = "Gestão administrativa", Version = "v1" });
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName ?? "v1";
        return groupName == docName;
    });

    foreach (var xmlFile in swaggerXmlFiles)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {token}"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default")!);
dataSourceBuilder.MapEnum<StatusOrdemServico>("status_ordem_servico");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource)
           .UseLowerCaseNamingConvention());

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? string.Empty;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVeiculoRepositorio, VeiculoRepositorio>();
builder.Services.AddScoped<IVeiculoService, VeiculoService>();

builder.Services.AddScoped<IServicoRepositorio, ServicoRepositorio>();
builder.Services.AddScoped<IServicoService, ServicoService>();

builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<EstoqueBaixaRealizadaEvent>, EstoqueBaixaRealizadaHandler>();
builder.Services.AddScoped<IDomainEventHandler<EstoqueAbaixoMinimoEvent>, EstoqueAbaixoMinimoHandler>();

builder.Services.AddScoped<IPecaRepositorio, PecaRepositorio>();
builder.Services.AddScoped<IPecaService, PecaService>();

builder.Services.AddScoped<IOrdemServicoRepositorio, OrdemServicoRepositorio>();
builder.Services.AddScoped<IOrdemServicoService, OrdemServicoService>();
builder.Services.AddScoped<IDomainEventHandler<OrdemServicoDiagnosticoFinalizadoEvent>, DiagnosticoFinalizadoHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrcamentoRespondidoEvent>, OrcamentoRespondidoHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrdemServicoIniciadaEvent>, OrdemServicoIniciadaHandler>();

// DD_AGENT_HOST é injetado no pod pelo admission controller do Datadog. Sem ele
// (execução local, docker-compose), as métricas são descartadas.
var datadogAgentHost = builder.Configuration["DD_AGENT_HOST"];
if (string.IsNullOrWhiteSpace(datadogAgentHost))
{
    builder.Services.AddSingleton<IMetricasService, MetricasDesabilitadasService>();
}
else
{
    builder.Services.AddSingleton<IDogStatsd>(_ =>
    {
        var statsd = new DogStatsdService();
        statsd.Configure(new StatsdConfig
        {
            StatsdServerName = datadogAgentHost,
            Prefix = DogStatsdMetricasService.Prefixo
        });
        return statsd;
    });
    builder.Services.AddSingleton<IMetricasService, DogStatsdMetricasService>();
}
// Antes da notificação por e-mail: uma falha no envio não impede o registro da métrica.
builder.Services.AddScoped<IDomainEventHandler<OrdemServicoStatusAlteradoEvent>, MetricasStatusOrdemServicoHandler>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDomainEventHandler<OrdemServicoStatusAlteradoEvent>, NotificacaoStatusOrdemServicoHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrcamentoRecusadoEvent>, NotificacaoStatusOrdemServicoHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TechChallenger API v1");
        options.SwaggerEndpoint("/swagger/GestaoAdministrativa/swagger.json", "Gestão administrativa");
    });
}

// Correlação primeiro, para o id estar no escopo de todos os logs abaixo, inclusive
// o log de request, que fica antes do ExceptionMiddleware para registrar o status final.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (http, _, ex) =>
        ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
        : http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
        : LogEventLevel.Information;
});
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
await app.RunAsync();
