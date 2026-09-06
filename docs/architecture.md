# Архитектура SmartLinks

## 1. Назначение и статус

SmartLinks состоит из двух приложений:

- `SmartLinks.Management.Api` — создание, изменение и публикация конфигураций;
- `SmartLinks.Redirect.Api` — разрешение коротких ссылок по опубликованной read-модели.

Основные требования к архитектуре:

- redirect-path не зависит от PostgreSQL и доступности Management;
- публикации передаются в Redirect асинхронно;
- обновление read-модели атомарно;
- новая Redirect-реплика получает трафик только после первоначальной синхронизации;
- последняя корректная read-модель сохраняется при временной ошибке синхронизации.

## 2. C4: System Context

```mermaid
flowchart LR
    configurator["Настройщик конфигурации<br/>[Person]"]
    visitor["Пользователь короткой ссылки<br/>[Person]"]
    smartLinks["SmartLinks<br/>[Software System]<br/>Управление правилами и выбор целевого URL"]
    geoLite["DB-IP Country Lite<br/>[External data source]<br/>База соответствия IP и страны"]

    configurator -->|"Создание, изменение и публикация конфигураций по HTTPS"| smartLinks
    visitor -->|"GET /{slug}"| smartLinks
    smartLinks -->|"302 Found или 404 Not Found"| visitor
    geoLite -.->|"Поставляется как локальный файл"| smartLinks
```

DB-IP Country Lite является внешним источником данных, но не сетевой зависимостью redirect-path. Redirect читает локальный MMDB-файл без запросов к DB-IP.

## 3. C4: Container

```mermaid
flowchart TB
    configurator["Настройщик<br/>[Person]"]
    visitor["Пользователь<br/>[Person]"]

    subgraph smartLinksBoundary["SmartLinks — Software System"]
        ingress["Traefik<br/>[Container: Ingress]<br/>TLS и публичная маршрутизация"]
        management["SmartLinks.Management.Api<br/>[Container: ASP.NET Core .NET 8]<br/>Write-side и публикация"]
        postgres[("PostgreSQL 16<br/>[Container: Database]<br/>Source of truth и change log")]
        redirect["SmartLinks.Redirect.Api<br/>[Container: ASP.NET Core .NET 8]<br/>2–3 read-side реплики"]
        geoLite[("DB-IP Country Lite<br/>[Container: File data store]<br/>Локальная GeoIP-база")]
    end

    configurator -->|"HTTPS, X-Api-Key для изменений"| ingress
    visitor -->|"HTTPS, GET /{slug}"| ingress
    ingress -->|"Публичные Management-маршруты"| management
    ingress -->|"Redirect-маршрут"| redirect
    management -->|"EF Core / Npgsql"| postgres
    redirect -->|"HTTP pull: snapshot и change feed<br/>внутренняя сеть"| management
    redirect -->|"Чтение файла"| geoLite
```

| Компонент | Ответственность |
| --- | --- |
| `SmartLinks.Management.Api` | Write-модель, валидация, публикация, snapshot и change feed |
| PostgreSQL | Редактируемое и опубликованное состояние, глобальная последовательность ревизий, append-only change log |
| `SmartLinks.Redirect.Api` | Локальная compiled read-модель и обработка `GET /{slug}` |
| `SmartLinks.RuleEngine` | Компиляция DSL, построение контекста и выбор правила |
| `SmartLinks.Contracts` | Контракты snapshot и change feed |
| Traefik | TLS и внешняя маршрутизация в целевом K3s-развёртывании |
| DB-IP Country Lite | Локальное определение страны по IP |

`SmartLinks.RuleEngine` и `SmartLinks.Contracts` являются общими библиотеками, а не отдельными C4-контейнерами.

## 4. Публикация конфигурации

```mermaid
sequenceDiagram
    autonumber
    actor Configurator as Настройщик
    participant Management as Management API
    participant Application as Application
    participant Database as PostgreSQL
    participant Worker as Redirect worker
    participant Store as Snapshot store

    Configurator->>Management: POST или PUT конфигурации с X-Api-Key
    Management->>Application: Создать или изменить SmartLink
    Application->>Application: Проверить slug, URL и приоритеты
    Application->>Database: Сохранить редактируемую модель
    Database-->>Application: Сохранено
    Application-->>Management: Выполнено
    Management-->>Configurator: Успешный HTTP-ответ

    Configurator->>Management: POST /api/smart-links/{id}/publish
    Management->>Application: PublishSmartLink
    Application->>Application: Скомпилировать DSL всех правил
    Application->>Database: Выполнить транзакцию публикации
    Database->>Database: Назначить глобальную revision
    Database->>Database: Обновить published state и добавить change log
    Database-->>Application: Зафиксированная revision
    Application-->>Management: Новая revision
    Management-->>Configurator: 200 OK с revision

    Note over Configurator,Store: Redirect обновляется асинхронно после завершения публикации

    Worker->>Management: GET /internal/configurations/changes?afterRevision=current
    Management->>Database: Прочитать следующий пакет
    Database-->>Management: Упорядоченные изменения
    Management-->>Worker: Change feed

    alt Непрерывная последовательность revision
        Worker->>Store: Скомпилировать и применить пакет
        Store->>Store: Атомарно заменить immutable-состояние
    else Разрыв revision или ошибка применения
        Worker->>Management: GET /internal/configurations/snapshot
        Management->>Database: Прочитать published state
        Database-->>Management: Snapshot и high-water revision
        Management-->>Worker: Полный snapshot
        Worker->>Store: Скомпилировать и атомарно заменить состояние
    end

    opt Management недоступен
        Note over Worker,Store: Текущий snapshot сохраняется, запрос повторяется с backoff и jitter
    end
```

Создание и изменение обновляют только редактируемую модель. Redirect получает новую конфигурацию только после публикации.

`PublishSmartLinkUseCase` компилирует DSL всех правил до записи. Затем `ConfigurationChangeLog` в одной транзакции добавляет change log, обновляет published state и фиксирует глобальную `revision`.

Параметры синхронизации по умолчанию:

| Параметр | Значение |
| --- | --- |
| `PollingInterval` | 5 секунд |
| `ChangeBatchSize` | 100 |
| `InitialRetryDelay` | 1 секунда |
| `MaximumRetryDelay` | 30 секунд |

## 5. Обработка redirect-запроса

```mermaid
sequenceDiagram
    autonumber
    actor Visitor as Пользователь
    participant Ingress as Traefik
    participant Middleware as SmartLinkRedirectMiddleware
    participant Snapshot as Snapshot store
    participant Context as UrlResolutionContextFactory
    participant Resolver as SmartLinkResolver

    Visitor->>Ingress: GET /{slug}
    Ingress->>Middleware: Запрос и клиентский IP
    Middleware->>Snapshot: Найти конфигурацию по slug

    alt Slug отсутствует
        Snapshot-->>Middleware: Не найден
        Middleware-->>Visitor: 404 Not Found через Traefik
    else Конфигурация найдена
        Snapshot-->>Middleware: Compiled SmartLinkConfiguration
        Middleware->>Context: Построить контекст
        Context->>Context: UTC-время, страна, устройство, браузер
        Context-->>Middleware: UrlResolutionContext
        Middleware->>Resolver: Resolve(configuration, context)

        alt Ссылка неактивна
            Resolver-->>Middleware: TargetUrl отсутствует
            Middleware-->>Visitor: 404 Not Found через Traefik
        else Ссылка активна
            Resolver->>Resolver: Проверить enabled-правила по priority
            Resolver->>Resolver: Выбрать первое совпадение или DefaultUrl
            Resolver-->>Middleware: TargetUrl
            Middleware-->>Visitor: 302 Found, Location, Cache-Control: no-store
        end
    end
```

`SmartLinkRedirectMiddleware` обрабатывает только одно-сегментные `GET /{slug}`. Маршруты `/health` и `/swagger` исключены. Поиск slug выполняется до построения контекста.

Redirect-path использует только локальную память и локальный MMDB-файл DB-IP Country Lite. Запросов к Management, PostgreSQL и внешнему GeoIP API нет.

## 6. Проектные проблемы сложности и решения

Основной источник нелинейного роста сложности — увеличение числа комбинаций времени, страны, устройства, браузера, логических операторов и приоритетов правил. Дополнительная сложность возникает при асинхронной доставке и конкурентном обновлении read-модели.

| Проблема | Проявление в SmartLinks | Решение |
| --- | --- | --- |
| Рост комбинаций условий | В одном сценарии используются mobile, Edge, UTC-интервал + Chrome и default URL; DSL также поддерживает `all`, `any` и `not` | Версионированный JSON DSL, рекурсивный `ConditionDslCompiler`, Composite-условия и независимые `IConditionFactory` |
| Рост центрального алгоритма | При прямой реализации новый признак потребовал бы изменения общего `if/switch` и существующих ветвей | `SmartLinkResolver` работает только с priority и `ICompiledCondition`; структура конкретного условия ему неизвестна |
| Расширение контекста | Добавление нового признака могло бы потребовать изменения общего DTO и всех мест его создания | Типизированные `IResolutionFeature` и отдельные `IResolutionContextContributor` |
| Пересечение User-Agent-маркеров | Edge и Opera содержат маркер Chrome; автоматизированный клиент может содержать маркеры браузера | Порядок распознавания локализован в `UserAgentBrowserResolver` и `UserAgentDeviceResolver` и покрыт отдельными тестами |
| Разные требования к записи и чтению | Публикации нужны валидация и транзакция; redirect-path не должен зависеть от сети и базы | CQRS: Management владеет write-моделью, Redirect — локальной compiled read-моделью |
| Конкурентное обновление проекции | При смене slug или применении пакета читатель не должен видеть промежуточное состояние | Новый `SnapshotState` полностью подготавливается заранее и публикуется через `Interlocked.CompareExchange` |
| Повторы и пропуски при polling | Change feed может содержать старые записи или разрыв после текущей revision | Глобальная монотонная revision, игнорирование повторов, проверка `current + 1`, полная загрузка snapshot при разрыве |
| Пустая проекция после старта | До первой загрузки существующий slug выглядел бы как отсутствующий | Readiness становится положительной только после первоначального snapshot |
| Недоступность Management | Синхронизация временно невозможна | Сохраняется последняя корректная проекция; HTTP-запрос повторяется с exponential backoff и jitter |
| Реальный IP за Traefik | Без forwarded headers определяется IP proxy; без ограничения доверия возможна подмена страны | `KnownNetworks`, `ForwardLimit = 1` и локальный `MaxMindClientLocationResolver` |
| Несколько pod на одном VPS | Реплики Redirect не защищают от отказа единственного узла | Явно заявлена только process-level и pod-level устойчивость, без host-level HA |

### 6.1. Локализация изменений

| Изменение | Требуемые изменения в коде или данных | Не затрагивается |
| --- | --- | --- |
| Новая комбинация существующих условий | JSON DSL и сценарные тесты | Compiler, resolver и middleware |
| Новый предикат над существующим feature | Condition, `IConditionFactory`, DI-регистрация и тесты | Composite, resolver и HTTP-слой |
| Новый источник данных | `IResolutionFeature`, contributor, condition, factory, DI и тесты | `UrlResolutionContextFactory`, resolver и алгоритм `all/any/not` |
| Другой URL, priority или состояние правила | Конфигурация SmartLink | Код Management и Redirect |
| Пропущенная revision | Полная перезагрузка snapshot в `ConfigurationSynchronizer` | Rule Engine и публичный redirect-контракт |

Архитектура локализует изменения в конкретном предикате, источнике контекста или механизме синхронизации. Число бизнес-комбинаций по-прежнему требует сценарных тестов.

## 7. CQRS и eventual consistency

| Аспект | Management: command side | Redirect: query side |
| --- | --- | --- |
| Модель | Редактируемый агрегат и published state | Compiled immutable snapshot |
| Хранилище | PostgreSQL | Память реплики |
| Оптимизация | Валидация и транзакционность | Локальное lock-free чтение |
| Согласованность | Строгая внутри транзакции публикации | Eventual consistency относительно Management |
| Восстановление | PostgreSQL — source of truth | Повторная загрузка полного snapshot |

Гарантии и ограничения:

- успешная публикация revision `N` не означает немедленное применение `N` всеми Redirect-репликами;
- read-your-writes через публичный redirect endpoint не гарантируется;
- разные реплики могут кратковременно использовать разные ревизии;
- внутри одной реплики новое состояние публикуется атомарно;
- устаревшая или повторная revision не откатывает проекцию;
- жёсткая верхняя граница задержки распространения при сбоях отсутствует.

## 8. Применённые паттерны

| Паттерн | Реализация | Роль в SmartLinks |
| --- | --- | --- |
| Interpreter | `ConditionDslCompiler` | Компиляция версионированного JSON DSL |
| Composite | `AllCondition`, `AnyCondition`, `NotCondition` | Вложенные комбинации условий через общий `ICompiledCondition` |
| Strategy | Условия времени, страны, устройства и браузера | Изоляция алгоритма отдельного предиката |
| Chain of Responsibility | Обход правил по `priority` в `SmartLinkResolver` | Выбор первого совпавшего enabled-правила |
| Specification | Реализации `ICompiledCondition` | Инкапсуляция проверок над `UrlResolutionContext` |
| CQRS | Management и Redirect | Разделение write-модели и оптимизированной read-модели |

Dependency Injection используется для регистрации реализаций, но не заявляется отдельным поведенческим паттерном. Command, Adapter и GoF Factory в перечень применённых паттернов не включены.

## 9. Сбои и эксплуатационные ограничения

| Ситуация | Поведение |
| --- | --- |
| Management недоступен | Redirect использует последнюю корректную проекцию |
| PostgreSQL недоступен | Readiness Management становится отрицательной; работающий Redirect продолжает обслуживание |
| Redirect не выполнил первоначальную загрузку | Liveness положительная, readiness отрицательная |
| Change feed содержит разрыв | Пакет не публикуется, загружается полный snapshot |
| Компиляция новой проекции завершается ошибкой | Текущее immutable-состояние сохраняется |
| Перезапускается одна Redirect-реплика | Остальные Ready-реплики могут принимать трафик |
| Отказывает VPS | Недоступны все компоненты |

Изменяющие endpoints Management защищены `X-Api-Key`. Внутренние endpoints snapshot и change feed не используют API-ключ и должны быть исключены из внешней Ingress-маршрутизации. PostgreSQL также не публикуется наружу.

Redirect доверяет `X-Forwarded-For` только для настроенных proxy-сетей. Целевые URL ограничены абсолютными адресами со схемой `http` или `https`. Ответ redirect содержит `Cache-Control: no-store`.

Целевая K3s-конфигурация использует один узел, 2–3 Redirect-реплики и один PostgreSQL с локальным PVC. Она не обеспечивает host-level high availability. Multi-node Kubernetes, HA PostgreSQL, распределённое хранилище, брокер сообщений и автоматическое масштабирование не входят в обязательный объём проекта.
