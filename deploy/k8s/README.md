# Развёртывание SmartLinks в K3s

Инструкция предназначена для одноузлового сервера с Ubuntu 24.04 LTS.

## Требования

Перед развёртыванием необходимо подготовить:

- сервер с публичным IPv4;
- два доменных имени для Management API и Redirect API;
- DNS-записи `A`, направленные на сервер;
- открытые входящие TCP-порты `80` и `443`;
- доступ к GitHub и GHCR;
- `git`, `curl`, `openssl`.

Порты PostgreSQL, Kubernetes API и kubelet не должны быть доступны из интернета.

Перед публикацией необходимо заменить домены в `deploy/k8s/ingress.yaml` и проверить immutable-теги контейнеров:

```bash
grep -R \
  --line-number \
  'image: ghcr.io/' \
  deploy/k8s
```

## Установка K3s

Команды выполняются на сервере с правами администратора.

```bash
install -d -m 700 /etc/rancher/k3s

printf '%s\n' \
  'write-kubeconfig-mode: "600"' \
  'secrets-encryption: true' \
  > /etc/rancher/k3s/config.yaml

chmod 600 /etc/rancher/k3s/config.yaml
```

Установить K3s:

```bash
curl -sfL https://get.k3s.io |
  INSTALL_K3S_VERSION="v1.36.4+k3s1" \
  INSTALL_K3S_EXEC="server" \
  sh -
```

Проверить готовность узла:

```bash
kubectl wait \
  --for=condition=Ready \
  node \
  --all \
  --timeout=180s

kubectl get nodes \
  --output wide
```

## Установка cert-manager

```bash
kubectl apply \
  --filename https://github.com/cert-manager/cert-manager/releases/download/v1.21.1/cert-manager.yaml

kubectl wait \
  --namespace cert-manager \
  --for=condition=Available \
  deployment \
  --all \
  --timeout=180s
```

## Подготовка репозитория

Клонировать подготовленную ветку проекта на сервер:

```bash
git clone \
  --branch dev \
  --single-branch \
  "https://github.com/your-account/SmartLinks.git" \
  /opt/smartlinks

cd /opt/smartlinks
```

URL репозитория необходимо заменить на фактический.

## Создание секретов

Файл `deploy/k8s/secrets.env` исключён из Git и не должен попадать в репозиторий.

```bash
umask 077

printf 'POSTGRES_PASSWORD=%s\nMANAGEMENT_API_KEY=%s\n' \
  "$(openssl rand -hex 32)" \
  "$(openssl rand -hex 32)" \
  > deploy/k8s/secrets.env

chmod 600 deploy/k8s/secrets.env
```

Проверить формат:

```bash
grep -Eq '^POSTGRES_PASSWORD=[0-9a-f]{64}$' deploy/k8s/secrets.env \
  && echo "POSTGRES_PASSWORD: valid" \
  || echo "POSTGRES_PASSWORD: invalid"

grep -Eq '^MANAGEMENT_API_KEY=[0-9a-f]{64}$' deploy/k8s/secrets.env \
  && echo "MANAGEMENT_API_KEY: valid" \
  || echo "MANAGEMENT_API_KEY: invalid"

git check-ignore -v deploy/k8s/secrets.env
```

## Настройка Traefik

Конфигурация Traefik применяется отдельно, поскольку ресурс находится в namespace `kube-system`.

```bash
kubectl apply \
  --filename deploy/k8s/traefik-config.yaml
```

Проверить сохранение внешнего IP клиента:

```bash
kubectl get service traefik \
  --namespace kube-system \
  --output=custom-columns='NAME:.metadata.name,EXTERNAL-TRAFFIC-POLICY:.spec.externalTrafficPolicy'
```

Значение `EXTERNAL-TRAFFIC-POLICY` должно быть `Local`. Это необходимо для корректного определения страны пользователя.

## Развёртывание приложения

Создать namespace:

```bash
kubectl apply \
  --filename deploy/k8s/namespace.yaml
```

Проверить манифесты через Kubernetes API:

```bash
kubectl apply \
  --dry-run=server \
  --kustomize deploy/k8s \
  >/dev/null \
  && echo "Kubernetes API validation: valid"
```

Применить ресурсы:

```bash
kubectl apply \
  --kustomize deploy/k8s
```

Дождаться готовности PostgreSQL:

```bash
kubectl wait \
  --namespace smartlinks \
  --for=condition=Ready \
  pod/smartlinks-postgres-0 \
  --timeout=180s
```

Дождаться завершения миграций:

```bash
kubectl wait \
  --namespace smartlinks \
  --for=condition=Complete \
  job/smartlinks-management-migrations \
  --timeout=180s

kubectl logs \
  --namespace smartlinks \
  job/smartlinks-management-migrations \
  --all-containers=true \
  --prefix=true
```

Дождаться запуска приложений:

```bash
kubectl rollout status \
  deployment/smartlinks-management \
  --namespace smartlinks \
  --timeout=180s

kubectl rollout status \
  deployment/smartlinks-redirect \
  --namespace smartlinks \
  --timeout=180s
```

Дождаться выпуска TLS-сертификатов:

```bash
kubectl wait \
  --namespace smartlinks \
  --for=condition=Ready \
  certificate/smartlinks-management-tls \
  certificate/smartlinks-redirect-tls \
  --timeout=180s
```

## Проверка результата

Проверить состояние ресурсов:

```bash
kubectl get pods,jobs \
  --namespace smartlinks \
  --output wide

kubectl get persistentvolumeclaims,services \
  --namespace smartlinks

kubectl get ingress,issuer,certificate \
  --namespace smartlinks
```

Ожидаемое состояние:

- PostgreSQL — `1/1 Running`;
- Management — `1/1 Running`;
- три pod Redirect — `1/1 Running`;
- миграционный Job — `Complete`;
- PVC PostgreSQL — `Bound`;
- TLS-сертификаты — `Ready`.

Проверить публичные health endpoints:

```bash
SMARTLINKS_MANAGEMENT_HOST="management.example.com"
SMARTLINKS_REDIRECT_HOST="go.example.com"

curl \
  --fail \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out 'Management readiness: %{http_code}\n' \
  "https://${SMARTLINKS_MANAGEMENT_HOST}/health/ready"

curl \
  --fail \
  --silent \
  --show-error \
  --output /dev/null \
  --write-out 'Redirect readiness: %{http_code}\n' \
  "https://${SMARTLINKS_REDIRECT_HOST}/health/ready"
```

Перед выполнением проверки необходимо заменить примерные домены на фактические. Оба запроса должны вернуть `200`.