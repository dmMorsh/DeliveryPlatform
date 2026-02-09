# Operational Playbooks

This is a minimal set of runbooks for common incidents. The goal is fast diagnosis and a safe first response.

## 1) DLQ Growth

**Symptoms**
- `dlq.events` volume increasing
- errors in consumers or `handler_failed`

**Immediate actions**
1. Open DLQ service:
   - `GET /dlq?limit=50`
2. Identify event types and originating topic (headers `event-type`, `original-topic`).
3. Check consumer logs for the same event-id.

**Decision**
- If the failure is transient and safe to retry, requeue:
  - `POST /dlq/requeue` with `partition`, `offset`
- If the failure is due to bad payload or non‑recoverable bug, keep in DLQ and open a ticket.

**Follow‑up**
- Add event type to `Kafka:PoisonEventTypes` if it is known to be non‑recoverable.
- Add alert if DLQ rate exceeds threshold.

## 2) Kafka Lag Increasing

**Symptoms**
- Consumer lag increasing
- delayed events, timeouts

**Immediate actions**
1. Check Kafka health and brokers status.
2. Verify consumer health:
   - service `/health/ready`
3. Verify DB connection pool/latency.

**Decision**
- Scale consumer instances if CPU/memory is saturated.
- If DB is bottleneck, scale DB or optimize queries.

**Follow‑up**
- Add alert on lag and on consumer restarts.

## 3) Payment Failures

**Symptoms**
- spike of `payment.failed` events
- provider timeouts

**Immediate actions**
1. Check provider status pages.
2. Check `PaymentService` logs for external API errors.
3. Validate credentials (`Payments__Sberbank` / `Payments__YooMoney`).

**Decision**
- If provider is down, switch traffic or disable provider.
- If malformed requests, rollback and reprocess from DLQ.

**Follow‑up**
- Add alert on provider error rate.

## 4) Order API Errors (5xx)

**Symptoms**
- elevated 5xx rate on `/api/orders`

**Immediate actions**
1. Check `OrderService` readiness.
2. Check DB health and connections.
3. Verify Kafka connectivity.

**Decision**
- Roll back last deployment if errors correlate with new release.
- Scale out if load spike.

## 5) Redis/Cache Outage

**Symptoms**
- Courier location updates fail

**Immediate actions**
1. Check Redis health.
2. Verify connection string.

**Decision**
- Restart Redis or fail over.

## 6) Identity/Auth Failures

**Symptoms**
- JWT validation errors

**Immediate actions**
1. Verify `Jwt__Key/Issuer/Audience`.
2. Check system clock drift.

**Decision**
- Rotate secrets if compromised.

