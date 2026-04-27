#!/bin/sh
set -e
# Kubernetes Single Step Instrumentation (admission controller) sets CORECLR_* and profiler path.
if [ -n "${CORECLR_PROFILER_PATH:-}" ] && [ "${CORECLR_ENABLE_PROFILING:-}" = "1" ]; then
  exec dotnet /app/StockPriceApi.dll "$@"
fi
# Docker Compose: use Datadog.Trace.Bundle assets published under /app/datadog
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
export DD_DOTNET_TRACER_HOME=/app/datadog
case "$(uname -m)" in
  aarch64|arm64) export CORECLR_PROFILER_PATH=/app/datadog/linux-arm64/Datadog.Trace.ClrProfiler.Native.so ;;
  *) export CORECLR_PROFILER_PATH=/app/datadog/linux-x64/Datadog.Trace.ClrProfiler.Native.so ;;
esac
exec dotnet /app/StockPriceApi.dll "$@"
