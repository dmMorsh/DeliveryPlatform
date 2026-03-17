# Delivery Platform

Микросервисная платформа доставки на .NET с событийной интеграцией через Kafka, общей инфраструктурой Postgres/Redis/Elasticsearch и наблюдаемостью через OpenTelemetry.

## Архитектура (коротко)

- Gateway API как единая точка входа.
- Сервисы общаются по HTTP/gRPC и через Kafka‑события.
- Для чтения есть отдельные read‑сервисы (CatalogReadService, OrderReadService).
- Redis используется для кэшей и live‑данных (например, локации курьеров).

## Сервисы

- **GatewayApi** — API‑шлюз/агрегация.
- **IdentityService** — аутентификация/токены.
- **CatalogService** — запись каталога.
- **CatalogReadService** — поиск/чтение каталога (Elasticsearch).
- **CartService** — корзина.
- **InventoryService** — остатки.
- **OrderService** — жизненный цикл заказов.
- **OrderReadService** — read‑модель заказов.
- **DeliveryService** — назначение и статусы доставки.
- **CourierService** — курьеры и их доступность.
- **LocationTrackingService** — gRPC‑трекинг локаций курьеров (Redis).
- **PaymentService** — платежи.
- **PaymentMockService** — заглушка платежей для дев/тестов.
- **NotificationService** — уведомления.
- **AnalyticsService** — потребитель событий/аналитика.
- **DlqService** — обработка DLQ.

## Стек и инфраструктура

- **.NET**: net9.0
- **PostgreSQL 15**, **Redis 7**, **Kafka 7.5**, **Elasticsearch 9.3**
- **Observability**: OpenTelemetry, Prometheus, Grafana, Tempo, Loki
- **gRPC** для стриминга локаций

## Репозиторий

- [src](/src) — все сервисы и общие библиотеки.
- [docker-compose.yml](/docker-compose.yml) — локальная инфраструктура и сервисы.
- [Tests.IntegrationTests](/src/Tests.IntegrationTests) — интеграционные тесты.
