#!/bin/sh
set -e
# Kubernetes: admission controller + Single Step Instrumentation set CORECLR_* and profiler paths.
# Docker Compose: no injection in this setup—run without the CLR profiler (no APM unless you add a tracer install).
exec dotnet /app/StockPriceApi.dll "$@"
