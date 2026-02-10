#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="${ROOT_DIR}/logs/smoke-checks.txt"
SOLUTION="${ROOT_DIR}/DeliveryPlatform.sln"

mkdir -p "${ROOT_DIR}/logs"
echo "Smoke checks started at $(date -u +"%Y-%m-%dT%H:%M:%SZ")" | tee "${OUTPUT}"

echo "ROOT_DIR=$ROOT_DIR" | tee "${OUTPUT}"
ls -la "$ROOT_DIR" | tee -a "${OUTPUT}"

echo "Checking solution file:" | tee -a "${OUTPUT}"
ls -la "$SOLUTION" | tee -a "${OUTPUT}"

dotnet --info | tee -a "${OUTPUT}"


echo "Building solution..." | tee -a "${OUTPUT}"
dotnet restore "$SOLUTION" >> "${OUTPUT}" 2>&1
dotnet build "$SOLUTION" --no-restore >> "${OUTPUT}" 2>&1

echo "Checking unit tests (if any)..." | tee -a "${OUTPUT}"

set +e
dotnet test "$SOLUTION" --list-tests >> "${OUTPUT}" 2>&1
HAS_TESTS=$?
set -e

if [ "$HAS_TESTS" -eq 0 ]; then
  dotnet test "$SOLUTION" >> "${OUTPUT}" 2>&1
else
  echo "No tests discovered; skipping." | tee -a "${OUTPUT}"
fi

echo "Smoke checks completed at $(date -u +"%Y-%m-%dT%H:%M:%SZ")" | tee -a "${OUTPUT}"