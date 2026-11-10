# BuildWorkflowAPI

ASP.NET Core 10 web application that lets you configure database connections and expose named APIs (stored procedures or SQL queries) at `/api/{route}` with API-key authentication.

## Features

- Connection management for SQL Server, MySQL, and PostgreSQL
- API definitions backed by stored procedures or SQL queries
- Input/output parameter mapping
- Per-API and global API keys
- Cookie-based admin authentication (seeded admin only)
- Optional SSO token login
- Audit logging of API calls (request/response/status/key)
- Google Console-inspired UI

## Run locally

```powershell
cd D:\Claude\BuildWorkflowAPI
dotnet build
dotnet run
```

Default URLs: `http://localhost:5268` and `https://localhost:7151`.

## Configuration

`appsettings.json`:

```json
{
  "AllowedHosts": "*",
  "Sso": { "ValidationUrl": "" },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=BuildWorkflow;Username=new_admin;Password=Test12345"
  }
}
```

The `Default` connection string is the metadata database used by EF Core.

## Database and seeding

Run PostgreSQL at the configured host/port. EF migrations are applied automatically at startup. Admin users are stored in `AppUsers` (avoids conflicts with other projects using a `Users` table).

Seed scripts in `sql/`:

- `seed-app-users.postgres.sql` — creates/updates `AppUsers` and seeds `admin` / `Password123!`
- `seed-sample-api.appdb.postgres.sql` — sample table/function and API
- `seed-sample-api.mssql.sql` — SQL Server sample table/procedure
- `seed-sample-api.postgres.sql` — PostgreSQL standalone sample

## Invoke a sample API

```bash
curl -X POST http://localhost:5268/api/app-sample-employees \
  -H "X-API-Key: sample-app-api-key" \
  -H "Content-Type: application/json" \
  -d '{"dept": "Engineering"}'
```

## Step-by-step guide

1. Sign in with seeded admin (`admin` / `Password123!`) or configured SSO token.
2. Open **Connections** and create a database connection (provider + connection string).
3. Open **API definitions** and create an API:
   - choose connection
   - choose command type (`StoredProcedure` or `Query`)
   - enter command text
   - set route (for example `get-users`)
4. Add parameters (name/type/required/default/output).
5. Generate at least one API key (per API or global).
6. Call the API:

```bash
curl -X POST http://localhost:5268/api/get-users \
  -H "X-API-Key: <your-key>" \
  -H "Content-Type: application/json" \
  -d '{"id": 42}'
```

7. Use API key in `X-API-Key` header or `api_key` query parameter.
8. Review calls in **API logs** (request/response/status/key/time).
9. For SSO, set `Sso:ValidationUrl` and call `/Sso/Login?token=<token>`.

## Security

- Vulnerable NuGet packages have been updated; `dotnet list package --vulnerable` should report none.
- Raw DB error messages are not exposed to API callers.
- Admin routes require authentication.
- API keys validate gateway calls and are logged.