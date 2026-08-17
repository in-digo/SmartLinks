# SmartLinks

[![CI](https://github.com/in-digo/SmartLinks/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/in-digo/SmartLinks/actions/workflows/ci.yml)

SmartLinks - учебный проект сервиса умных ссылок для курса «Архитектура и шаблоны проектирования».

Сервис будет выбирать целевой URL короткой ссылки в зависимости от времени, страны пользователя, типа устройства, браузера и логических комбинаций условий.

## Архитектура

Проект разделён на два приложения согласно CQRS:

- `SmartLinks.Management.Api` управляет конфигурацией умных ссылок и является владельцем write-модели;
- `SmartLinks.Redirect.Api` обслуживает переходы, используя локальную immutable read-модель.

Redirect-сервис не должен обращаться к Management или PostgreSQL во время пользовательского перехода. Обновление read-модели будет выполняться через периодическую HTTP pull-синхронизацию.

## Структура репозитория

```text
src/
├── BuildingBlocks/
│   ├── SmartLinks.Contracts/
│   └── SmartLinks.RuleEngine/
├── Management/
│   ├── SmartLinks.Management.Domain/
│   ├── SmartLinks.Management.Application/
│   ├── SmartLinks.Management.Infrastructure/
│   └── SmartLinks.Management.Api/
└── Redirect/
    ├── SmartLinks.Redirect.Application/
    ├── SmartLinks.Redirect.Infrastructure/
    └── SmartLinks.Redirect.Api/

tests/
├── SmartLinks.RuleEngine.Tests/
├── SmartLinks.Management.UnitTests/
├── SmartLinks.Management.IntegrationTests/
├── SmartLinks.Redirect.UnitTests/
└── SmartLinks.Redirect.IntegrationTests/