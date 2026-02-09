#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="${ROOT_DIR}/logs/smoke-checks.txt"

mkdir -p "${ROOT_DIR}/logs"
echo "Smoke checks started at $(date -u +"%Y-%m-%dT%H:%M:%SZ")" | tee "${OUTPUT}"

echo "Building solution..." | tee -a "${OUTPUT}"
dotnet build "${ROOT_DIR}" >> "${OUTPUT}"

echo "Checking unit tests (if any)..." | tee -a "${OUTPUT}"
if dotnet test "${ROOT_DIR}" --list-tests | grep -q .; then
  dotnet test "${ROOT_DIR}" >> "${OUTPUT}"
else
  echo "No tests discovered; skipping." | tee -a "${OUTPUT}"
fi

echo "Smoke checks completed at $(date -u +"%Y-%m-%dT%H:%M:%SZ")" | tee -a "${OUTPUT}"
