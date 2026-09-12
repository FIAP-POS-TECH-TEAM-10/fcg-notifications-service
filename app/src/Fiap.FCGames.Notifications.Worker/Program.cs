using Fiap.FCGames.Notifications.Worker.Consumers;
using Fiap.FCGames.Notifications.Worker.Middleware;
using MassTransit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Consumers — coração do serviço (apenas geram logs estruturados). Transporte escolhido
// pela config (Messaging:Provider=Sqs usa Amazon SQS/SNS na AWS; senão, se RabbitMQ:Host
// estiver definido, usa RabbitMQ como hoje — local/docker-compose). Mesmos consumers e
// nomes de fila/endpoint ("notifications-usuario-criado", "notifications-pagamento-processado"
// — convenção do CLAUDE.md) nos dois transportes.
var messagingProvider = builder.Configuration["Messaging:Provider"];
var rabbitHost = builder.Configuration["RabbitMQ:Host"];

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UsuarioCriadoEventoConsumer>();
    x.AddConsumer<PagamentoProcessadoEventoConsumer>();

    if (string.Equals(messagingProvider, "Sqs", StringComparison.OrdinalIgnoreCase))
    {
        // Credenciais: cadeia padrão do AWS SDK (Task Role do ECS via
        // AWS_CONTAINER_CREDENTIALS_RELATIVE_URI, injetado automaticamente pelo agente).
        var region = builder.Configuration["AWS:Region"] ?? "sa-east-1";

        x.UsingAmazonSqs((ctx, cfg) =>
        {
            cfg.Host(region, h => { });

            cfg.ReceiveEndpoint("notifications-usuario-criado", e =>
            {
                e.ConfigureConsumer<UsuarioCriadoEventoConsumer>(ctx);
            });

            cfg.ReceiveEndpoint("notifications-pagamento-processado", e =>
            {
                e.ConfigureConsumer<PagamentoProcessadoEventoConsumer>(ctx);
            });
        });
    }
    else if (!string.IsNullOrWhiteSpace(rabbitHost))
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

            cfg.Host(rabbitHost, "/", h =>
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
    }
    else
    {
        // Sem broker configurado — satisfaz a DI, consumers ficam sem receber nada.
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
});

// Web host mínimo: existe apenas para expor /health ao liveness/readiness probe do k8s.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCorrelationId();

app.MapHealthChecks("/health");

await app.RunAsync();
