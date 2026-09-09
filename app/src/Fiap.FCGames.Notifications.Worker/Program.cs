using Fiap.FCGames.Notifications.Worker.Consumers;
using Fiap.FCGames.Notifications.Worker.Middleware;
using MassTransit;
using Serilog;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Consumers do RabbitMQ — coração do serviço (apenas geram logs estruturados).
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UsuarioCriadoEventoConsumer>();
    x.AddConsumer<PagamentoProcessadoEventoConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ReceiveEndpoint("notifications-usuario-criado", e =>
        {
            e.ConfigureConsumer<UsuarioCriadoEventoConsumer>(ctx);
        });

        cfg.ReceiveEndpoint("notifications-pagamento-processado", e =>
        {
            e.ConfigureConsumer<PagamentoProcessadoEventoConsumer>(ctx);
        });
    });
});

// Web host mínimo: existe apenas para expor /health ao liveness/readiness probe do k8s.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCorrelationId();

// Middleware para métricas HTTP (latência, status code, etc.)
app.UseRouting();
app.UseHttpMetrics();

// Endpoint padrão /metrics
app.MapMetrics();

app.MapHealthChecks("/health");

await app.RunAsync();
