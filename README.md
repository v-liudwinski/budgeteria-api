# Budgeteria API

ASP.NET Core 8 REST API for Budgeteria — a family budget tracking application.

## Tech Stack

- **.NET 8** — ASP.NET Core Web API
- **PostgreSQL** — via EF Core + Npgsql
- **Auth0** — JWT Bearer authentication
- **MailKit** — transactional email (invitation flow)
- **Health checks** — `/health` and `/health/ready` endpoints

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance (local or remote)
- Auth0 tenant with an API configured

## Getting Started

### 1. Configure secrets

Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store sensitive values locally:

```bash
cd src/Budgeteria.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=budgeteria;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Auth0:Domain" "your-tenant.us.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://api.budgeteria.online/"
dotnet user-secrets set "Email:SmtpPass" "your-smtp-password"
```

`appsettings.json` contains non-secret defaults (SMTP host/port/user, CORS origins). Override them via `appsettings.Local.json` (gitignored) or environment variables if needed.

### 2. Run

```bash
cd src/Budgeteria.Api
dotnet run
```

The API starts on `http://localhost:5000`. Migrations are applied automatically on startup.

### 3. Test

```bash
dotnet test
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/register` | Register / sync Auth0 user |
| POST | `/api/auth/invite` | Send family invitation email |
| GET/POST | `/api/expenses` | List / create expenses |
| PUT/DELETE | `/api/expenses/{id}` | Update / delete expense |
| GET/POST | `/api/plans` | List / create budget plans |
| PUT/DELETE | `/api/plans/{id}` | Update / delete plan |
| GET | `/health` | Liveness check |
| GET | `/health/ready` | Readiness check (includes DB) |

## Project Structure

```
src/
  Budgeteria.Api/
    Controllers/     # Auth, Expenses, Plans
    Data/            # EF Core DbContext
    Dtos/            # Request/response shapes
    Migrations/      # EF Core migrations
    Models/          # Entity classes
    Services/        # Auth0UserService, EmailService, PlanAnalysisService
tests/
  Budgeteria.Api.Tests/
```

## Configuration Reference

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Auth0:Domain` | Auth0 tenant domain |
| `Auth0:Audience` | Auth0 API audience |
| `Email:SmtpHost` | SMTP server host |
| `Email:SmtpPort` | SMTP server port |
| `Email:SmtpUser` | SMTP username |
| `Email:SmtpPass` | SMTP password (use secrets) |
| `Email:FromEmail` | Sender address |
| `Email:FrontendUrl` | Frontend base URL (for invitation links) |
| `Cors:AllowedOrigins` | Array of allowed CORS origins |
