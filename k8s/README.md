# Notifications Worker - Kubernetes Deployment

Este diretório contém os manifestos Kubernetes para deploy do microsserviço Notifications Worker.

## Recursos

- **Deployment**: Define os Pods do Worker
- **Service**: Expõe o Worker para health checks
- **ConfigMap**: Configurações não sensíveis
- **Secret**: Credenciais do RabbitMQ

## Deploy

### 1. Build da Imagem Docker

```bash
docker build -t fcg-notifications-worker:latest .
```

### 2. Aplicar Manifestos

> **Atenção:** crie o Secret (seção abaixo) **antes** de aplicar e não use `kubectl apply -f k8s/` na pasta inteira: isso aplicaria o `secret.yaml` modelo (valores `REPLACE_ME`) por cima do Secret real.

```bash
kubectl apply -f k8s/configmap.yaml -f k8s/deployment.yaml -f k8s/service.yaml
```

### 3. Verificar Status

```bash
kubectl get pods -n fcgames -l app=notifications-worker
kubectl logs -n fcgames -l app=notifications-worker -f
```

## Configurações

### ConfigMap (configmap.yaml)
- `rabbitmq-host`: Host do RabbitMQ
- `aspnetcore-urls`: URLs que a aplicação escuta

### Secret (secret.yaml)
- `rabbitmq-username`: Usuário do RabbitMQ
- `rabbitmq-password`: Senha do RabbitMQ

**IMPORTANTE**: Altere os valores dos Secrets em produção!

O `secret.yaml` versionado é só um **modelo** (valores `REPLACE_ME`). Crie o Secret real com as suas credenciais:

```bash
kubectl create secret generic notifications-worker-secret -n fcgames \
  --from-literal=rabbitmq-username="$RABBITMQ__USERNAME" \
  --from-literal=rabbitmq-password="$RABBITMQ__PASSWORD"
```

## Acesso

### Port Forward para health check

```bash
kubectl port-forward -n fcgames svc/notifications-worker 5004:80
```

Acesse: http://localhost:5004/health

## Eventos Consumidos

- **UserCreatedEvent**: Envia e-mail de boas-vindas
- **PaymentProcessedEvent**: Envia e-mail de confirmação de compra (se aprovado)

## Simulação de E-mails

Este serviço **simula** o envio de e-mails logando as mensagens no console. Para ver os "e-mails" enviados, verifique os logs:

```bash
kubectl logs -n fcgames -l app=notifications-worker -f
```

## Dependências

- RabbitMQ (deve estar rodando no namespace fcgames)

## Nota

Este é um serviço Worker puro (consumer), sem endpoints REST. O Service é criado apenas para possibilitar health checks.
