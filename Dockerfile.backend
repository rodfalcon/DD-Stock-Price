# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Install curl, download and install the Tracer
RUN apt-get update \
    && apt-get install -y curl \
    && mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v2.55.0/datadog-dotnet-apm-2.55.0.arm64.tar.gz \
    && tar -xzf datadog-dotnet-apm-2.55.0.arm64.tar.gz -C /opt/datadog \
    && rm datadog-dotnet-apm-2.55.0.arm64.tar.gz \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Enable the tracer
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" \
    CORECLR_PROFILER_PATH="/opt/datadog/Datadog.Trace.ClrProfiler.Native.so" \
    DD_INTEGRATIONS="/opt/datadog/integrations.json" \
    DD_TRACE_DEBUG=1 \
    DD_DOTNET_TRACER_HOME="/opt/datadog"

# Use the official ASP.NET Core build image
FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0 AS build
WORKDIR /src
COPY StockPriceApi.csproj ./
RUN dotnet restore "StockPriceApi.csproj"
COPY . .
WORKDIR /src
RUN dotnet build "StockPriceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StockPriceApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StockPriceApi.dll"]
