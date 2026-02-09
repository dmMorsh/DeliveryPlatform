# Alerting

This file describes recommended alerts. Thresholds should be tuned after baseline data.

## Service Health

- **Service down**: readiness endpoint failing for > 2 minutes.
- **Restart loops**: container restarts > 3 in 10 minutes.

## HTTP/API

- **5xx rate**: > 1% of requests for 5 minutes.
- **p95 latency**: > 1s for 10 minutes (Gateway, Order, Payment).

## Kafka

- **Consumer lag**: lag > 10,000 for 10 minutes.
- **DLQ rate**: > 10 messages/5 minutes.
- **Retry rate**: > 50 messages/5 minutes.

## Database

- **DB connection errors**: > 5 errors/5 minutes.
- **Slow queries**: p95 > 500ms.

## Payment

- **Provider errors**: > 2% in 10 minutes.
- **Authorize/Capture failures**: > 1% in 10 minutes.

## Infrastructure

- **Disk > 80%**
- **CPU > 80% for 10 minutes**
- **Memory > 85% for 10 minutes**

