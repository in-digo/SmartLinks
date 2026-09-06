# SmartLinks

[![CI](https://github.com/in-digo/SmartLinks/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/in-digo/SmartLinks/actions/workflows/ci.yml)

SmartLinks — сервис коротких ссылок с настраиваемой маршрутизацией. Для одного короткого адреса можно задать несколько целевых URL и выбирать нужный по стране пользователя, типу устройства, браузеру, интервалу времени в UTC или комбинации этих условий.

Конфигурации создаются и изменяются через Management API. После публикации Redirect API автоматически получает новую ревизию и отвечает на `GET /{slug}` перенаправлением `302 Found`. Если ни одно правило не подошло, используется адрес по умолчанию.

## Документация

- [Архитектура и проектные решения](docs/architecture.md)
- [Развёртывание в K3s](deploy/k8s/README.md)
- [Postman-коллекция](postman/SmartLinks.postman_collection.json)

После локального запуска Swagger Management API доступен по адресу [http://127.0.0.1:5200/swagger](http://127.0.0.1:5200/swagger).

## Локальный запуск

Требуются Docker с Docker Compose и OpenSSL. Команды выполняются из корня репозитория.

Создать локальные секреты:

```bash
install -d -m 700 deploy/docker/.secrets

umask 077

openssl rand -hex 32 \
  > deploy/docker/.secrets/postgres-password

openssl rand -hex 32 \
  > deploy/docker/.secrets/management-api-key

chmod 0444 \
  deploy/docker/.secrets/postgres-password \
  deploy/docker/.secrets/management-api-key
```

Запустить PostgreSQL, миграции и оба приложения:

```bash
docker compose \
  --file deploy/docker/compose.yaml \
  up \
  --build \
  --wait \
  --wait-timeout 180
```

Локальные адреса:

| Компонент | Адрес |
| --- | --- |
| Management API | `http://127.0.0.1:5200` |
| Redirect API | `http://127.0.0.1:5025` |
| Swagger | `http://127.0.0.1:5200/swagger` |

Прочитать API-ключ для следующих примеров:

```bash
SMARTLINKS_API_KEY="$(
  tr -d '\r\n' \
    < deploy/docker/.secrets/management-api-key
)"
```

Остановить сервисы без удаления данных PostgreSQL:

```bash
docker compose \
  --file deploy/docker/compose.yaml \
  down
```

## HTTP API

Изменяющие запросы Management API требуют заголовок `X-Api-Key`. Чтение конфигурации и переход по короткой ссылке доступны без API-ключа.

| Метод | Маршрут | Назначение | API-ключ |
| --- | --- | --- | --- |
| `POST` | `/api/smart-links` | Создать редактируемую конфигурацию | да |
| `GET` | `/api/smart-links/{id}` | Получить редактируемую конфигурацию | нет |
| `PUT` | `/api/smart-links/{id}` | Полностью заменить редактируемую конфигурацию | да |
| `POST` | `/api/smart-links/{id}/publish` | Проверить DSL и опубликовать конфигурацию | да |
| `GET` | `/{slug}` | Перейти по опубликованной ссылке через Redirect API | нет |

### Создание ссылки

```bash
curl \
  --request POST \
  --url http://127.0.0.1:5200/api/smart-links \
  --header 'Content-Type: application/json' \
  --header "X-Api-Key: ${SMARTLINKS_API_KEY}" \
  --data '{
    "slug": "campaign",
    "defaultUrl": "https://example.com/default",
    "isActive": true,
    "rules": [
      {
        "priority": 10,
        "isEnabled": true,
        "targetUrl": "https://example.com/mobile",
        "conditionDsl": "{\"dslVersion\":1,\"condition\":{\"type\":\"device\",\"parameters\":{\"deviceType\":\"mobile\"}}}"
      },
      {
        "priority": 20,
        "isEnabled": true,
        "targetUrl": "https://example.com/gb-chrome",
        "conditionDsl": "{\"dslVersion\":1,\"condition\":{\"all\":[{\"type\":\"country\",\"parameters\":{\"countryCode\":\"GB\"}},{\"type\":\"browser\",\"parameters\":{\"browser\":\"chrome\"}}]}}"
      }
    ]
  }'
```

Успешный запрос возвращает `201 Created`, идентификатор ссылки в теле и адрес ресурса в заголовке `Location`:

```json
{
  "id": "11111111-2222-3333-4444-555555555555"
}
```

Сохранить полученный идентификатор для следующих запросов:

```bash
SMARTLINK_ID="11111111-2222-3333-4444-555555555555"
```

### Чтение ссылки

```bash
curl \
  --silent \
  --show-error \
  "http://127.0.0.1:5200/api/smart-links/${SMARTLINK_ID}"
```

Ответ содержит текущую редактируемую конфигурацию. Она может отличаться от уже опубликованной версии, которую использует Redirect API.

### Изменение ссылки

`PUT` полностью заменяет конфигурацию, включая весь массив правил. Правила, не переданные в запросе, будут удалены из редактируемой версии.

```bash
curl \
  --request PUT \
  --url "http://127.0.0.1:5200/api/smart-links/${SMARTLINK_ID}" \
  --header 'Content-Type: application/json' \
  --header "X-Api-Key: ${SMARTLINKS_API_KEY}" \
  --data '{
    "slug": "campaign",
    "defaultUrl": "https://example.com/default",
    "isActive": true,
    "rules": [
      {
        "priority": 10,
        "isEnabled": true,
        "targetUrl": "https://example.com/firefox",
        "conditionDsl": "{\"dslVersion\":1,\"condition\":{\"type\":\"browser\",\"parameters\":{\"browser\":\"firefox\"}}}"
      }
    ]
  }'
```

Успешный запрос возвращает `204 No Content`. Изменения ещё не влияют на переходы, пока конфигурация не опубликована.

### Публикация ссылки

```bash
curl \
  --request POST \
  --url "http://127.0.0.1:5200/api/smart-links/${SMARTLINK_ID}/publish" \
  --header "X-Api-Key: ${SMARTLINKS_API_KEY}"
```

Management API проверяет DSL всех правил и возвращает номер новой глобальной ревизии:

```json
{
  "revision": 42
}
```

Номер ревизии зависит от количества предыдущих публикаций. Redirect API применяет опубликованную конфигурацию автоматически. Стандартный интервал синхронизации — 5 секунд.

### Переход по короткой ссылке

Следующий запрос имитирует Firefox и соответствует правилу из примера обновления:

```bash
sleep 6

curl \
  --silent \
  --show-error \
  --output /dev/null \
  --max-time 15 \
  --user-agent 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Gecko/20100101 Firefox/129.0' \
  --write-out '%{http_code} %{redirect_url}\n' \
  http://127.0.0.1:5025/campaign
```

Ожидаемый результат:

```text
302 https://example.com/firefox
```

Если ссылка активна, но ни одно включённое правило не совпало, Redirect API вернёт `302` на `defaultUrl`. Неизвестный или неактивный `slug` возвращает `404 Not Found`.

## Правила маршрутизации

Каждое правило содержит:

| Поле | Назначение |
| --- | --- |
| `priority` | Целое число. Чем меньше значение, тем раньше проверяется правило. Приоритеты внутри одной ссылки должны быть уникальны |
| `isEnabled` | Отключённое правило сохраняется, но не участвует в выборе URL |
| `targetUrl` | Абсолютный HTTP- или HTTPS-адрес для совпавшего правила |
| `conditionDsl` | JSON DSL версии 1, сериализованный в строку |

Алгоритм выбора URL:

1. Неактивная ссылка возвращает `404 Not Found`.
2. Включённые правила проверяются по возрастанию `priority`.
3. Используется `targetUrl` первого совпавшего правила.
4. Если совпадений нет, используется `defaultUrl`.

### Формат DSL

До сериализации в строку условие имеет следующий корневой формат:

```json
{
  "dslVersion": 1,
  "condition": {
    "type": "device",
    "parameters": {
      "deviceType": "mobile"
    }
  }
}
```

В HTTP-запросе этот JSON передаётся строкой, поэтому двойные кавычки экранируются:

```json
{
  "conditionDsl": "{\"dslVersion\":1,\"condition\":{\"type\":\"device\",\"parameters\":{\"deviceType\":\"mobile\"}}}"
}
```

Структура DSL проверяется при публикации. Это позволяет сохранить незавершённую редактируемую конфигурацию, но опубликовать некорректное условие нельзя.

### Страна

`countryCode` следует задавать двухбуквенным кодом ISO 3166-1 alpha-2, например `GB`, `DE` или `NL`:

```json
{
  "dslVersion": 1,
  "condition": {
    "type": "country",
    "parameters": {
      "countryCode": "GB"
    }
  }
}
```

Определение страны требует настроенной GeoIP-базы и исходного IP клиента. В стандартном K3s-развёртывании GeoIP включён; результат зависит от актуальности данных провайдера.

### Тип устройства

Поддерживаемые значения `deviceType`: `mobile`, `tablet`, `desktop`, `unknown`.

```json
{
  "dslVersion": 1,
  "condition": {
    "type": "device",
    "parameters": {
      "deviceType": "mobile"
    }
  }
}
```

Тип устройства определяется по заголовку `User-Agent`.

### Браузер

Поддерживаемые значения `browser`: `chrome`, `firefox`, `safari`, `edge`, `opera`, `unknown`.

```json
{
  "dslVersion": 1,
  "condition": {
    "type": "browser",
    "parameters": {
      "browser": "firefox"
    }
  }
}
```

Браузер определяется по заголовку `User-Agent`.

### UTC-интервал

Интервал задаётся в ISO 8601. Начало `fromUtc` включается, окончание `toUtc` не включается: `[fromUtc, toUtc)`.

```json
{
  "dslVersion": 1,
  "condition": {
    "type": "utcTime",
    "parameters": {
      "fromUtc": "2026-09-01T00:00:00Z",
      "toUtc": "2026-10-01T00:00:00Z"
    }
  }
}
```

### Логические комбинации

DSL поддерживает три логические операции:

- `all` — должны совпасть все вложенные условия;
- `any` — должно совпасть хотя бы одно вложенное условие;
- `not` — инвертирует одно вложенное условие.

`all` и `any` принимают непустой массив условий. Операции можно вкладывать друг в друга. Например, правило для пользователя из Великобритании с мобильным устройством или планшетом, но не с браузером Opera:

```json
{
  "dslVersion": 1,
  "condition": {
    "all": [
      {
        "type": "country",
        "parameters": {
          "countryCode": "GB"
        }
      },
      {
        "any": [
          {
            "type": "device",
            "parameters": {
              "deviceType": "mobile"
            }
          },
          {
            "type": "device",
            "parameters": {
              "deviceType": "tablet"
            }
          }
        ]
      },
      {
        "not": {
          "type": "browser",
          "parameters": {
            "browser": "opera"
          }
        }
      }
    ]
  }
}
```

## Коды ответов

| Код | Когда возвращается |
| --- | --- |
| `200 OK` | Конфигурация прочитана или опубликована |
| `201 Created` | Ссылка создана |
| `204 No Content` | Ссылка изменена |
| `302 Found` | Redirect API выбрал целевой URL |
| `400 Bad Request` | Некорректны URL, правила или DSL при публикации |
| `401 Unauthorized` | Нет корректного API-ключа для изменяющего запроса |
| `404 Not Found` | Запрошенный ID или опубликованный slug не найден либо ссылка неактивна |
| `409 Conflict` | Указанный slug уже занят |

Ошибки Management API возвращаются в формате Problem Details.

## Тесты и покрытие

Запустить сборку и тесты:

```bash
dotnet tool restore
dotnet restore SmartLinks.sln
dotnet build SmartLinks.sln \
  --configuration Release \
  --no-restore
dotnet test SmartLinks.sln \
  --configuration Release \
  --no-build \
  --no-restore \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory artifacts/coverage-results
```

Сформировать HTML-отчёт и проверить минимальное покрытие строк 90%:

```bash
dotnet reportgenerator \
  "-reports:artifacts/coverage-results/**/coverage.cobertura.xml" \
  "-targetdir:artifacts/coverage-report" \
  "-reporttypes:Html;TextSummary" \
  "-assemblyfilters:+SmartLinks.*;-*.Tests" \
  "-filefilters:-*/Migrations/*" \
  "--minimumCoverageThresholds:lineCoverage=90"
```

Запустить полный Postman E2E-сценарий для уже поднятого Docker Compose:

```bash
docker compose \
  --file deploy/docker/compose.yaml \
  --profile e2e \
  run \
  --build \
  --rm \
  newman
```

## Ограничения

- публикация применяется в Redirect асинхронно, поэтому новая конфигурация появляется не мгновенно;
- точность правил страны зависит от GeoIP-базы и корректного сохранения исходного IP клиента;
- базовое K3s-развёртывание использует один узел: несколько реплик Redirect защищают от сбоя отдельного процесса, но не от потери сервера.
