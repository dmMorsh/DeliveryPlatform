#!/usr/bin/env bash
set -e

get_method_body() {
  local content="$1"
  local method="$2"

  echo "$content" | sed -n "/void $method(/,/^    }/p"
}

TS=$(date +"%Y%m%d%H%M%S")

SERVICES=(
  CartService
  CatalogService
  CourierService
  OrderService
  PaymentService
  InventoryService
  DeliveryService
)

get_contexts() {
  case "$1" in
    CartService)
      echo "CartDbContext"
      ;;
    CatalogService)
      echo "CatalogDbContext"
      ;;
    CourierService)
      echo "CourierDbContext"
      ;;
    OrderService)
      echo "OrderDbContext"
      ;;
    PaymentService)
      echo "PaymentDbContext PaymentShardMapDbContext"
      ;;
    InventoryService)
      echo "InventoryDbContext"
      ;;
    DeliveryService)
      echo "DeliveryDbContext"
      ;;
    *)
      echo ""
      ;;
  esac
}

for SERVICE in "${SERVICES[@]}"; do
  echo "=============================="
  echo "Service: $SERVICE"
  echo "=============================="

  INFRA="../../src/$SERVICE/$SERVICE.Infrastructure"
  API="../../src/$SERVICE/$SERVICE.Api"
  MIGRATIONS="$INFRA/Persistence/Migrations"

  CONTEXTS=$(get_contexts "$SERVICE")

  for CONTEXT in $CONTEXTS; do
    MIGRATION="Auto_${TS}_${CONTEXT}"

    echo "→ DbContext: $CONTEXT"
    echo "→ Migration: $MIGRATION"

    dotnet ef migrations add "$MIGRATION" \
      --context "$CONTEXT" \
      --project "$INFRA" \
      --startup-project "$API" \
      --output-dir Persistence/Migrations

    FILE=$(ls -t "$MIGRATIONS"/*.cs \
      | grep -v Designer \
      | grep -v Snapshot \
      | head -n 1)

    CONTENT=$(cat "$FILE")

    UP=$(get_method_body "$CONTENT" "Up")
    DOWN=$(get_method_body "$CONTENT" "Down")

    if ! echo "$UP$DOWN" | grep -q "migrationBuilder."; then
      echo "⚠ Empty migration detected for $CONTEXT → removing"

      dotnet ef migrations remove \
        --context "$CONTEXT" \
        --project "$INFRA" \
        --startup-project "$API"
    else
      echo "✓ Migration contains changes"
    fi
  done
done
