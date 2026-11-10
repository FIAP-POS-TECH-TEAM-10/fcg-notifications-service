# fcg-notifications-service (worker de notificações)

Notificações do FCGames **no ambiente local** (`docker compose` com RabbitMQ). Consome os eventos
e escreve logs JSON simulando os e-mails; não tem banco nem API, só `/health`.
Na AWS quem faz isso é a Lambda [`fcg-notifications-lambda`](https://github.com/FIAP-POS-TECH-TEAM-10/fcg-notifications-lambda).

## Eventos

| Evento | Fila | Log (`tipo`) |
|---|---|---|
| `UsuarioCriadoEvento` | `notifications-usuario-criado` | `email-boas-vindas` (destinatário, usuarioId, correlationId) |
| `PagamentoProcessadoEvento` | `notifications-pagamento-processado` | Aprovado: `email-confirmacao-compra` (jogo, valor) · Rejeitado: `email-pagamento-rejeitado` (jogo, motivo) |

Contratos: [`fcg-integration-events`](https://github.com/FIAP-POS-TECH-TEAM-10/fcg-integration-events).

## Endpoints

| Endpoint | Acesso | O que faz |
|---|---|---|
| `GET /health` | anônimo | health check |

## Variáveis de ambiente

| Variável | Obrigatória | O que é |
|---|---|---|
| `RabbitMQ__Host` · `RabbitMQ__Username` · `RabbitMQ__Password` | sim (local) | broker; sem usuário/senha o worker não sobe |
| `Messaging__Provider` | não | `Sqs` = SQS/SNS; vazio + `RabbitMQ__Host` = RabbitMQ; nenhum = em memória |
| `AWS__Region` | não (`sa-east-1`) | região do SQS/SNS |

## Rodar local

Jeito recomendado: `docker compose up` no
[`fcg-orchestration`](https://github.com/FIAP-POS-TECH-TEAM-10/fcg-orchestration) e
`docker compose logs -f notifications-worker`. Só este serviço:

```bash
export NUGET_AUTH_TOKEN=$(gh auth token) RABBITMQ__HOST=localhost RABBITMQ__USERNAME=<usuario> RABBITMQ__PASSWORD=<senha>
dotnet run --project app/src/Fiap.FCGames.Notifications.Worker    # /health em http://localhost:5004
```

Para Kind/Docker Desktop local, ver [`k8s/README.md`](k8s/README.md).
