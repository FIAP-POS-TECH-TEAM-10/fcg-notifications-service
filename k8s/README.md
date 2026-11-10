# notifications-worker em Kubernetes local (Kind / Docker Desktop)

Só para cluster local. Na AWS as notificações são a Lambda `fcg-notifications-lambda`.

1. Crie o Secret real (o `secret.yaml` daqui é só modelo, com `REPLACE_ME`):

```bash
kubectl create secret generic notifications-worker-secret -n fcgames \
  --from-literal=rabbitmq-username="$RABBITMQ__USERNAME" \
  --from-literal=rabbitmq-password="$RABBITMQ__PASSWORD"
```

2. Aplique os demais manifests (não use `kubectl apply -f k8s/`: aplicaria o modelo por cima do Secret):

```bash
kubectl apply -f k8s/configmap.yaml -f k8s/deployment.yaml -f k8s/service.yaml
```

3. Veja os logs das notificações:

```bash
kubectl logs -n fcgames -l app=notifications-worker -f
```

Precisa do RabbitMQ no namespace `fcgames` (manifests em `fcg-orchestration/k8s`).
