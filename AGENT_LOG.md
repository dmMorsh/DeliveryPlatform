# NOTE: DB migrations for any schema/read‑store changes are the user's responsibility. This agent will not apply migrations.

# Agent Log

This file is intended for critical notes and architectural observations made by assistants (agents). Only high‑impact items should be recorded.

## 2026-02-28
- Performed initial project analysis. Solution is a microservices suite running on .NET with PostgreSQL, Kafka, Redis and Elasticsearch.
- **CatalogService** already uses CQRS: Elasticsearch for search lake + a small relational DB for writes. Redis used as a product cache. Good design for high volume.
- **OrderService** has a separate read database (`order_read` schema) with denormalized order and kitchen slots tables. Projection contains many order/delivery fields but currently _delivery data is incomplete_ (missing some parts of the delivery payload that need to be added later). An in‑process memory TTL cache (5 sec) is used for individual order queries. Kitchen slot repository has unit test.
- **InventoryService** uses a dedicated read DB plus a Redis cache for stock items. Projections and indexes look sane.
- **CartService** reads directly from the write database; writes are not split. Redis is used for caching carts (TTL 1 h). Cache invalidation is handled on checkout; other events currently no‑ops because customer id not known. Potential gap: if read load grows, consider moving to separate read store or store cart→customer mapping for invalidation.
- Other services (`DeliveryService`, `CourierService`, `PaymentService`, `IdentityService`, `NotificationService`, etc.) currently use a single PostgreSQL database and do **not** have separate read models or caches (except `DeliveryService` uses Redis for assignment queue and courier activity store). This may become a bottleneck under 50 k+ RPC traffic. Evaluate common query patterns:
  - Courier availability/location queries – could benefit from a fast key‑value store (Redis or geospatial index).
  - Delivery listings / status dashboards – read replica or dedicated read schema could be added.
  - Payment transaction lookups – might require a read‑optimized store or caching of recent transactions.
  - Identity lookups / token validation – caching at API gateway or dedicated read cache recommended.
- **DeliveryService** already uses Redis heavily; read flows still hit the single DeliveryDbContext. Consider adding a read database with precomputed delivery views if UI queries grow.
- **PaymentService** shards the DB for writes, but no read caching. High‑traffic payment status requests might need caches or separate reporting store.
- No service currently uses Elasticsearch aside from catalog; search heavy domains should be considered (e.g. orders search, delivery routes, etc.).

These observations will guide further investigation. Adjust schema fields and caching strategies when new performance requirements arise.

### 2026-02-28 (update)
- Added plan to extend `OrderReadModel` with delivery/courier fields: `CourierName`, `CourierPhone`, `EstimatedDeliveryAt`, `EstimatedArrivalMinutes`.
- Propagated courier info through domain event `OrderAssignedDomainEvent` and integration mapping so assignment events include name/phone (prevents data loss between services).
- Added indexes for `CourierId` and `CourierName` on the `order_read` read store to support fast courier-based queries.

**User-facing Order read fields (recommended)**
- **OrderId**: unique id for the order.
- **OrderNumber**: human-friendly identifier.
- **Status**: order lifecycle status (use stable shared enum).
- **From/To address + coordinates**: for map and routing.
- **AssignedAt / AcceptedAt / ReadyAt / DeliveredAt**: timeline timestamps.
- **CourierId, CourierName, CourierPhone**: courier identity and contact.
- **EstimatedDeliveryAt / EstimatedArrivalMinutes**: ETA for customer-facing UI.
- **DeliveryZoneName / DeliveryFeeMultiplier**: pricing and region info.
- **Items, quantities, price, total (CostCents, Currency)**: order contents and cost.
- **IsReadyForDelivery**: quick flag for pickup readiness.
- **Courier current location (lat/long) and lastSeen**: optional, for live tracking (can be kept in Redis/Location service).

Record rationale: these fields support the typical customer UI (status timeline, contact courier, ETA, cost, items) and enable fast reads without joining multiple domain tables. Store high-frequency changing location/eta in a KV store (Redis) and keep denormalized snapshot fields in the read DB for durability and fallback.
## Инфраструктура и топология (текущее состояние)

DB Миграции: управляются пользователем, не автоматизированы.

### Stack технологий
- **RDBMS**: PostgreSQL 15 (один инстанс, шардирование на уровне приложения для PaymentService)
- **Cache/Live store**: Redis 7 (один инстанс в Docker; в k8s deployment 1-реплика, нет кластеризации)
- **Message broker**: Apache Kafka 7.5.0, один брокер
- **Observability**: OpenTelemetry (OTLP gRPC on 4317), Prometheus, Grafana, Tempo, Loki
- **SDK**: .NET 8+, gRPC, Serilog

### LocationTrackingService (gRPC сервис для локаций курьеров)
- Порт: 5127 (local), 8080 (container)
- Зависимости: Redis (only, no DB needed)
- **Методы**:
  - `UpdateLocation()` — single-request обновление локации
  - `StreamLocation()` — bidirectional streaming (для мобилы рекомендуется)
  - `GetCourierLocation()` — fetch текущей позиции из Redis
  - `GetCourierLocationHistory()` — история с subsampling (1 точка/мин, limit 2000)
- **Redis ключи**:
  - `courier:{id}:location` (JSON, TTL=24h) — live position
  - `courier:{id}:history` + `courier:{id}:history:last_ts` (list + TS guard, TTL=7d)
  - Pub/sub channel: `courier.location.updated` (есть, но consumers пока отсутствуют)
- **Subsampling logic**: Lua script контролирует частоту (не запишет чаще чем `HistorySampleInterval` = 1 минута)

### Другие сервисы
- **CourierService**: интегрирован с LocationTrackingService (gRPC клиент), Redis для active couriers
- **DeliveryService**: интегрирован с LocationTrackingService, Redis для assignment queue + courier activity
- **OrderService (read projection)**: пока не интегрирован с локациями, только статусы ордеров
- Все используют Kafka для event streaming

### Docker-stack
- 1x PostgreSQL, 1x Redis, 1x Kafka (Zookeeper), OTEL collector
- Health checks: readiness + liveness probes
- Все сервисы подконектены к одному Redis (no geo-sharding yet)

### k8s конфиги (текущие)
- `redis-deployment.yaml`: Deployment (1 replica), Service (ClusterIP)
  - БЕЗ: Statefulset, persistent volumes, Redis Cluster config
- `locationtracking-deployment.yaml`: Deployment (1 replica), Service, health probes
  - БЕЗ: HPA, affinity rules, multiple zones

### Проблемы и узкие места
1. **Redis single-node**: нет HA, нет шардирования, не масштабируется 50k+ RPC
2. **LocationTrackingService**: 1 инстанс — SPOF (single point of failure)
3. **Частота обновлений**: ~20с от каждого курьера
   - Kafka spam если писать каждое обновление (нежелательно)
   - Решение: храним только в Redis, история батчится раз в минуту (Lua script)
4. **Геораспределение**: отсутствует
   - На production нужны 2+ Redis в разных регионах/AZ
   - Сервисы, зная свою геозону, должны использовать ближайший Redis (DNS round-robin или явный конфиг)
5. **Мониторинг**: OpenTelemetry настроен, но нет оповещений для локаций (lag, loss)

---

## Архитектура для live-позиций курьеров (proposed)

### Принципы проектирования
1. **Избегаем Kafka spam**: каждые 20s × тысячи курьеров = миллионы событий/мин → избыточно
2. **High availability**: обновление локации не должно рушить систему, если Redis unavailable кратко (client caches, fallback to prev known position)
3. **Геораспределение**: Redis sharded by GeoZone, каждый DC/region имеет свой Redis cluster
4. **Real-time but lossy**: если мобила теряет соединение, не страшно (клиент скажет старые данные)
5. **Async батчинг**: Kafka используется только для важных events (assignment, ETA, delivery completion), не для raw locations

### Архитектурный поток

```
┌─────────────────────────────────────────────────────────────────┐
│ COURIER MOBILE APP                                              │
│ - Sends location every ~20s (gRPC UpdateLocation stream)        │
│ - Local HTTP cache: last-known position + timestamp             │
│ - Retry if network fails                                        │
└──────────────────────┬──────────────────────────────────────────┘
                       │ gRPC StreamLocation()
                       ▼
        ┌──────────────────────────────────────┐
        │ LocationTrackingService (N instances)│
        │ - Auth: JWT or device cert           │
        │ - Rate limit: max 10 req/min/courier │
        │ - Log + metrics (latency, updates/s) │
        └──────────────┬───────────────────────┘
                       │
          ┌────────────┴─────────────┐
          │ Write to Redis per zone  │
          │ - GEOADD + HSET + TTL    │
          │ - Subsampling: 1/min     │
          └────────────┬─────────────┘
                       ▼
    ┌─────────────────────────────────────┐
    │ Redis Cluster (per GeoZone)         │
    │ courier:{id}:location (TTL=60s)     │
    │ courier:{id}:history (TTL=7d)       │
    │ couriers:geo (GEOHASH)              │
    └────┬─────────────────┬──────────────┘
         │                 │
    ┌────▼─────────┐  ┌────▼──────────────────┐
    │ On Demand    │  │ Async Batch (1/min)   │
    │ Read Requests│  │ → Kafka → projections│
    │ (clients)    │  │ → OrderRead snapshot │
    └─────────────┘  └─────────────────────────┘
         │                  │
    ┌────▼─────────────────▼──────────┐
    │ OrderService/DeliveryService   │
    │ - Consume location.batch event  │
    │ - Update OrderReadModel ETA     │
    │ - Push to clients               │
    └────────────────────────────────┘
```

### Детали реализации

**1. LocationTrackingService улучшения**:
- [ ] Добавить rate limiter (per-courier async semaphore, max N updates per minute)
- [ ] Добавить sequence/version для ordering при retransmit
- [ ] Добавить geo-zone detection (входящий запрос → определяем zone из client IP или явно в gRPC metadata)
- [ ] Добавить metrics/traces: update latency, queue depth, % dropped

**2. Redis schema (per zone)**:
```
courier:{id}:location = {
  "CourierId": "...",
  "Latitude": 55.75,
  "Longitude": 37.61,
  "Timestamp": "2026-02-28T10:30:45Z",
  "Accuracy": 5,
  "Source": "gps",
  "OrderId": "..." (optional)
}  # TTL = 60s (live)

courier:{id}:meta = {
  assignedOrderId: "...",
  lastSeen: "2026-02-28T10:30:45Z",
  speed: 12.5,
  heading: 90,
  zone: "spb-01"
} # TTL = 90s

couriers:geo = GEOADD result (geo index) #TTL = 60s
```

**3. Batch / Analytics (every 60s)**:
- No raw location coordinates or per-update position data are to be published to Kafka.
- A BatchProcessor may optionally publish aggregated, non-identifying metrics (counts, density, anonymized heatmaps) for analytics. Any published artifact MUST NOT contain raw latitude/longitude or per-courier snapshots.
- Services that need ETA or routing decisions should query Redis (live snapshot) or call a dedicated ETA service; they must not rely on Kafka to carry raw position data.

**4. Consumers & analytics guidance**:
- Do not use Kafka as a transport for live courier positions.
- Analytics consumers may receive aggregate metrics or anonymized summaries (no per-courier coordinates).
- `OrderReadModel` must not store current locations or position snapshots; it should contain only delivery status and other durable order fields.

**6. Клиентские запросы (on-demand)**:
- Клиент запрашивает текущую позицию: `GetCourierLocation(courierId)`
- gRPC сервис читает из Redis, возвращает свежую (<60s)
- WebApp push notifications при обновлении (через SignalR/WebSocket или polling)

### Геораспределение в k8s

**Чёрновик k8s конфига**:
```yaml
# Redis StatefulSet per zone
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: redis-cluster-spb-01
  namespace: delivery
spec:
  serviceName: redis-cluster-spb-01
  replicas: 3
  selector:
    matchLabels:
      app: redis-cluster
      zone: spb-01
  template:
    metadata:
      labels:
        app: redis-cluster
        zone: spb-01
    spec:
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - podAffinityTerm:
                labelSelector:
                  matchExpressions:
                    - key: app
                      operator: In
                      values: [redis-cluster]
                topologyKey: kubernetes.io/hostname
      containers:
        - name: redis
          image: redis:7
          command:
            - /bin/sh
            - -c
            - |
              redis-server --cluster-enabled yes \
                --cluster-config-file /data/nodes.conf \
                --appendonly yes
          ports:
            - containerPort: 6379
              name: redis
            - containerPort: 16379
              name: cluster
          volumeMounts:
            - name: data
              mountPath: /data
  volumeClaimTemplates:
    - metadata:
        name: data
      spec:
        accessModes: [ReadWriteOnce]
        resources:
          requests:
            storage: 100Gi
---
apiVersion: v1
kind: Service
metadata:
  name: redis-cluster-spb-01
spec:
  clusterIP: None
  selector:
    zone: spb-01
  ports:
    - port: 6379
      targetPort: 6379
```

**LocationTrackingService (per zone)**:
```yaml
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: location-tracking-spb-01
  namespace: delivery
spec:
  replicas: 3
  selector:
    matchLabels:
      app: location-tracking
      zone: spb-01
  template:
    metadata:
      labels:
        app: location-tracking
        zone: spb-01
    spec:
      affinity:
        nodeAffinity:
          requiredDuringSchedulingIgnoredDuringExecution:
            nodeSelectorTerms:
              - matchExpressions:
                  - key: zone
                    operator: In
                    values: [spb-01]
      containers:
        - name: location-tracking
          image: your-registry/location-tracking:latest
          env:
            - name: Redis__Connection
              value: "redis-cluster-spb-01:6379"
            - name: GeoZone__ZoneId
              value: "spb-01"
```

### Альтернатива: на случай если geo-sharding сейчас излишен
- Пока используем single Redis (как сейчас)
- LocationTrackingService read_only для клиентов — масштабируется горизонтально за LB
- Батч-коробки собираются за LB (один инстанс для батча)
- Когда traffic растёт: переходим на Redis cluster + geo-sharding (migration plan готов выше)

### Мониторинг
- Metrics: `location_updates_per_second`, `location_redis_write_latency_ms`, `batch_events_published_per_min`, `stale_position_count` (timeout > 60s)
- Traces: latency gRPC UpdateLocation → Redis write
- Alerts: Redis connection errors, batch lag > 2 min, stale position rate > 5%