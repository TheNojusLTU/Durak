# Job Application Tracker

A Software Engineering course project for tracking job applications.

- `JobTracker`: ASP.NET Core (.NET 10) backend API with PostgreSQL, EF Core, and Identity.
- `JobTracker.Web`: separate React + TypeScript frontend using Vite.

Planned features include accounts, application tracking, search, interviews, and dashboard statistics.

## Setup

Requires .NET 10, PostgreSQL, Node.js 22.x, and npm 10+.

Run commands from the repository root. Start PostgreSQL and create a database named `jobtracker`. Set your local connection string, replacing the username and password as needed:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=jobtracker;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project JobTracker/JobTracker.csproj
```

Apply the existing database migrations:

```bash
dotnet ef database update --project JobTracker/JobTracker.csproj --startup-project JobTracker/JobTracker.csproj
```

Keep database credentials out of the repository.

## Run

Start the API:

```bash
dotnet run --project JobTracker/JobTracker.csproj --launch-profile http
```

Start the frontend in another terminal:

```bash
npm --prefix JobTracker.Web install
npm --prefix JobTracker.Web run dev
```

Open the frontend URL printed by Vite. Requests to `/api` are forwarded to `http://localhost:5099`, so local development does not need browser CORS configuration.

## API Foundation (KAN-69)

- Health: `GET http://localhost:5099/api/health` returns **200** and `{"status":"healthy"}`. It checks the API host, not the database.
- Swagger: `http://localhost:5099/swagger` in Development; OpenAPI JSON is at `/openapi/v1.json`.
- Errors: API errors use JSON Problem Details with a request path and trace ID.

The server no longer hosts Blazor pages. The old form routes `/account/login`, `/account/logout`, `/account/register`, and `/account/applications/save` return **404**. Their UI files remain for later cleanup.

Database models and migrations are preserved. Cookie authentication APIs are implemented in KAN-70; see [authentication endpoints, CSRF setup, and API checks](docs/authentication.md). The React frontend now provides registration, login, and cookie-backed session handling against that API. KAN-71 provides application CRUD, filtering, and pagination endpoints; connecting those endpoints to the dashboard remains follow-up work.

## Checks

```bash
dotnet build
npm --prefix JobTracker.Web run lint
npm --prefix JobTracker.Web run build
```
