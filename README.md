# DevOpsPlatform

DevOpsPlatform is a diploma MVP for viewing a distributed system as one operational topology. It joins project and environment metadata, components and declared dependencies, deployment history, current runtime state, and operational events. Reverse dependency traversal shows which components may be affected by a failure.

The repository is a monorepo. The API is an ASP.NET Core 10 modular monolith with Domain, Application, Infrastructure, Persistence, and API projects. PostgreSQL stores the model; an in-memory SSE bus pushes live updates to a Next.js/React Flow UI. The bus is suitable for one API instance.

## Prerequisites

- .NET 10 SDK and Node.js 22 with npm for separate processes
- PostgreSQL 17, or Docker with Compose for the full stack

## Docker Compose

1. Copy `.env.example` to `.env`. The values are local demo defaults; change the password for any shared environment.
2. Run `docker compose up --build`.
3. Open <http://localhost:3000>. API: <http://localhost:5000>. Development OpenAPI document: <http://localhost:5000/openapi/v1.json>.

The Development API applies the checked-in EF migration and seeds Commerce Demo on first start. PostgreSQL data persists in the `postgres_data` volume. Run `docker compose down` to stop; adding `-v` also removes demo data.

## Separate processes

Start PostgreSQL with a database named `devops_platform` and a user with access to it. Then:

```powershell
$env:ConnectionStrings__Postgres = 'Host=localhost;Port=5432;Database=devops_platform;Username=devops;Password=devops_local'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/src/DevOpsPlatform.Api --urls http://localhost:5000
```

In another terminal:

```powershell
cd frontend/web
npm ci
$env:NEXT_PUBLIC_API_URL = 'http://localhost:5000'
$env:NEXT_PUBLIC_ENABLE_DEMO = 'true'
npm run dev
```

To apply migrations explicitly (required outside Development), set `ConnectionStrings__Postgres` and run:

```powershell
dotnet ef database update --project backend/src/DevOpsPlatform.Persistence --startup-project backend/src/DevOpsPlatform.Api
```

No production migration runs automatically. Configure `FrontendOrigin` for the web origin. Secrets belong in environment variables or a development secret store, not Git.

## Demo workflow

1. Open Commerce Demo → Production → Topology.
2. Select Payment Service. Its seeded health is Degraded; inspect metrics, version, dependencies, events, and deployments.
3. Click **Analyze potential impact**. Order Service is direct; API Gateway and Web Frontend are indirect.
4. With `NEXT_PUBLIC_ENABLE_DEMO=true` and the API in Development, click **Restore health**, then **Simulate degradation**. The node and recent events update through SSE without a page reload.

The buttons call development-only API endpoints. Alternatively, update any component through `PUT /api/projects/{projectId}/environments/{environmentId}/components/{componentId}/runtime`.

## Checks

```powershell
dotnet restore backend/DevOpsPlatform.slnx
dotnet build backend/DevOpsPlatform.slnx
dotnet test backend/DevOpsPlatform.slnx
cd frontend/web
npm ci
npm run lint
npm run typecheck
npm run build
```

`docker compose config` checks the Compose file. The automated backend tests cover graph traversal, seeded EF queries, and HTTP endpoint flows using the in-memory provider. PostgreSQL integration tests remain to be added; the API and full Compose stack should be smoke tested against PostgreSQL before a release.

## Scope

This is an architectural baseline, not a CI runner, Kubernetes control plane, general telemetry store, or root-cause engine. Dependencies are declared through the API and seed data; current runtime state is submitted through the ingestion endpoint. Impact results express potential effect, not guaranteed failure or causation.
