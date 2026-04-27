# DD-Stock-Price

Sample **ASP.NET Core** API plus **React** frontend that loads observability data into **Datadog** (logs, APM traces, unified service tagging, optional RUM). A background worker periodically pulls quotes for **DDOG**, **DT**, and **NEWR** from Alpha Vantage and stores them in **SQLite** (`stockprices.db`).

---

## What you can do in the app

| Action | How |
|--------|-----|
| Open the UI | Use the URLs below (Docker Compose or Kubernetes). |
| See competitor prices | The UI calls `GET /api/StockPrice/competitors`. |
| Inspect one symbol | `GET /api/StockPrice/{symbol}` (e.g. `DDOG`) returns the latest row or `404` if none yet. |
| Comparisons | `GET /api/StockPrice/compare/{symbol}` returns price deltas over several windows. |

After startup, wait for at least one background fetch cycle (default **30 minutes**) or hit the API once data exists; until then, single-symbol GETs may return **404**.

---

## Run with Docker Compose (local)

**Prerequisites:** Docker, an `~/sandbox.docker.env` (or adjust `docker-compose.yml`) with your Datadog API key for the agent.

1. Build and start:

   ```bash
   docker compose build
   docker compose up -d
   ```

2. Open the app:

   - **Frontend:** [http://localhost:3000](http://localhost:3000)
   - **Backend API (direct):** [http://localhost:5261](http://localhost:5261)  
     Example: [http://localhost:5261/api/StockPrice/competitors](http://localhost:5261/api/StockPrice/competitors)

3. **Datadog agent** listens on host port **8126** for traces from the backend (see `docker-compose.yml`).

---

## Run on Kubernetes (e.g. Docker Desktop)

**Prerequisites:** `kubectl`, cluster access, Datadog Agent installed (e.g. Helm chart + `datadog-values.yaml` in this repo), API key in a secret such as `datadog-agent-secrets`.

1. Build images locally (tags used by the manifests):

   ```bash
   docker build -f Dockerfile.backend -t stockprice-backend:jsonlog .
   docker build -f Dockerfile.frontend -t stockprice-frontend:prod -f Dockerfile.frontend .
   ```

2. Apply workloads:

   ```bash
   kubectl apply -f deployment.yaml
   ```

3. Open the UI:

   - Frontend `Service` is **LoadBalancer** on port **8080**. On **Docker Desktop**, that is usually [http://localhost:8080](http://localhost:8080).

4. After code changes, rebuild the backend image and restart the deployment so the node picks up the new image (especially if you reuse the same tag):

   ```bash
   docker build -f Dockerfile.backend -t stockprice-backend:jsonlog .
   kubectl rollout restart deployment/stockprice-backend
   ```

---

## Datadog: logs and APM

- **Services:** `stock-price-api` (API), `stock-price-frontend` (UI), plus cluster/agent services.
- **Log Explorer:** filter with `service:stock-price-api`, `source:csharp`, or `env:production` (adjust for your env). Include **Info** as well as **Error** if you expect normal request logs.
- **Logs ↔ traces:** correlation needs JSON logs, `DD_LOGS_INJECTION`, unified `DD_ENV` / `DD_SERVICE` / `DD_VERSION`, and (for background work) active spans—see `deployment.yaml` and `Services/StockPriceFetcherService.cs`.

Screenshot from Datadog **Log Explorer** (structured `stock-price-api` log with service, cluster, and trace-friendly fields):

![Datadog Log Explorer – stock-price-api logs](docs/images/datadog-log-explorer-correlation.png)

---

## Repository layout (short)

| Path | Role |
|------|------|
| `StockPriceApi.csproj`, `Program.cs`, `Startup.cs` | .NET 8 API, Serilog JSON to stdout |
| `deployment.yaml` | Backend, frontend, DB, Services, namespace labels |
| `datadog-values.yaml` | Example Helm values for the Datadog Agent |
| `docker-compose.yml` | Local stack with agent, frontend, backend, optional SQL container |
| `stock-price-frontend/` | React app served via nginx in production image |

---

## Security note

Do not commit real API keys. Use environment files or Kubernetes secrets (this repo expects Datadog keys via secret / `env_file`, not hard-coded in git).
