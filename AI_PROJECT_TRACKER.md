# AI Project Tracker: BuildWorkflowAPI

This document is the AI-facing source of truth for the `BuildWorkflowAPI` .NET web project. It describes the current codebase so a future AI model can understand the architecture, data flow, configuration, and safe edit points without rediscovering everything.

## 1. Overview

- **Project name:** BuildWorkflowAPI
- **Solution file:** `BuildWorkflowAPI.slnx`
- **Project file:** `BuildWorkflowAPI.csproj`
- **Type:** ASP.NET Core MVC web application with a JSON API gateway endpoint
- **Framework:** .NET 10 (`net10.0`)
- **Purpose:** A lightweight "API builder" UI and runtime. Users configure database connections, define named API endpoints backed by SQL stored procedures or SQL queries, declare input/output parameters, and issue API keys. External callers then invoke `/api/{route}` with an API key; the runtime executes the configured SQL command against the configured connection and returns result sets/output parameters as JSON.
- **Auth model:** No application authentication is wired in the current codebase. Controllers and views are open. Authorization middleware is registered but no auth scheme/policy is configured.
- **Status:** Initial version with a single EF Core migration (`Initial`) and Razor UI for Connections and APIs.

## 2. Technology Stack

| Layer | Technology | Notes |
| --- | --- | --- |
| Runtime | .NET 10 | `Microsoft.NET.Sdk.Web` |
| Web | ASP.NET Core MVC | Controllers + Razor views |
| API | ASP.NET Core minimal-ish controller route | `ApiGatewayController` handles `/api/{*route}` |
| Database (app metadata) | PostgreSQL | EF Core via `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 |
| Target databases | SQL Server, MySQL, PostgreSQL | Runtime connections created per `DbProvider` using `Microsoft.Data.SqlClient`, `MySqlConnector`, `Npgsql` |
| ORM | EF Core | `AppDbContext`, auto-migrate at startup (best effort) |
| Frontend | Razor views + Bootstrap 5 + jQuery | Light theme navbar in `_Layout.cshtml` |
| Validation | DataAnnotations + jQuery validation unobtrusive | Standard MVC template assets |

## 3. Solution Structure

```
BuildWorkflowAPI/
  Program.cs                         // composition root, routing, auto-migrate
  BuildWorkflowAPI.csproj             // net10.0, packages
  appsettings.json                   // PostgreSQL connection string
  appsettings.Development.json        // logging defaults only

  Controllers/
    HomeController.cs                // Home/Privacy/Error
    ConnectionsController.cs          // CRUD for ApiConnection
    ApisController.cs                // CRUD for ApiDefinition + params + keys
    ApiGatewayController.cs           // runtime invocation endpoint

  Data/
    AppDbContext.cs                  // EF Core context
    Migrations/                      // Initial migration + snapshot

  Models/
    ApiConnection.cs                 // connection entity + nested entities
    Enums.cs                         // DbProvider, CommandType
    ErrorViewModel.cs                // MVC error model

  Services/
    ApiExecutor.cs                   // SQL execution engine

  Views/
    Home/...
    Connections/                     // Index/Create/Edit
    Apis/                            // Index/Create/Edit/Details
    Shared/_Layout.cshtml             // navbar, bootstrap, jquery

  wwwroot/                           // static CSS/JS/libs
  Properties/launchSettings.json     // http 5268, https 7151
```

## 4. Configuration

`appsettings.json` currently configures only the metadata PostgreSQL database:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=BuildWorkflow;Username=new_admin;Password=Test12345"
  }
}
```

Notes:
- There is no separate Ollama, auth, or feature configuration. This project does not use AI locally; it executes configured SQL.
- `appsettings.Development.json` only sets logging levels.
- Database connection strings for target SQL providers are stored as plain text in `ApiConnection.ConnectionString`.

## 5. Run Locally

```powershell
cd D:\Claude\BuildWorkflowAPI
dotnet build
dotnet run
```

Launch profiles:
- `http`: `http://localhost:5268`
- `https`: `https://localhost:7151;http://localhost:5268`

Database:
- PostgreSQL must be available at `localhost:5433`.
- Schema is created/updated by `db.Database.Migrate()` in `Program.cs`, wrapped in try/catch. If migration fails, the error is printed to console but app still starts.
- No seed data is created by `Program.cs`.

## 6. Domain Model

### `ApiConnection`

Represents a configured database connection.

| Property | Type | Notes |
| --- | --- | --- |
| `Id` | int | PK |
| `Name` | string (max 100, required) | Display name |
| `Provider` | `DbProvider` enum | `MsSql`, `MySql`, `Postgres` |
| `ConnectionString` | string (required) | Target DB connection string |
| `ApiDefinitions` | collection | Dependent APIs |

### `ApiDefinition`

Represents a named, route-addressable API.

| Property | Type | Notes |
| --- | --- | --- |
| `Id` | int | PK |
| `Name` | string (max 100, required) | Display name |
| `Route` | string (max 100, required, unique) | Matched against incoming `route` |
| `HttpMethod` | string (max 10) | Default `"POST"`; informational/metadata, not enforced by gateway |
| `ConnectionId` | int (required, FK) | Target connection |
| `CommandType` | `CommandType` enum | `StoredProcedure` or `Query` |
| `CommandText` | string (required) | SQL command text/SP name |
| `Description` | string? | Optional |
| `CreatedAt` | DateTime (UTC) | Set on creation |
| `Parameters` | collection | Input/output parameters |
| `ApiKeys` | collection | Scoped keys |

### `ApiParameter`

| Property | Type | Notes |
| --- | --- | --- |
| `Id` | int | PK |
| `ApiDefinitionId` | int | FK, cascade delete |
| `Name` | string (max 100, required) | Parameter name; should include DB prefix if needed? See executor notes |
| `DbType` | string (max 50) | Default `"string"`; one of int/integer, long/bigint, double/float, decimal, bool/boolean, datetime/date, guid, else string |
| `IsOutput` | bool | Output parameter |
| `IsRequired` | bool | Default true |
| `DefaultValue` | string? | Used when input is missing |
| `ApiDefinition` | navigation | Parent |

### `ApiKey`

| Property | Type | Notes |
| --- | --- | --- |
| `Id` | int | PK |
| `ApiDefinitionId` | int? | `null` means global key reusable for any API |
| `Name` | string (max 200, required) | Key label |
| `Key` | string (required) | Generated as 32-hex GUID (`Guid.NewGuid().ToString("N")`) |
| `IsActive` | bool | Default true |
| `CreatedAt` | DateTime (UTC) | Set on creation |

### Enums

- `DbProvider`: `MsSql = 0`, `MySql = 1`, `Postgres = 2`
- `CommandType`: `StoredProcedure = 0`, `Query = 1`

## 7. Database Context and Migration

`AppDbContext` (`Data/AppDbContext.cs`):
- DbSets: `Connections`, `ApiDefinitions`, `ApiParameters`, `ApiKeys`
- Relationships:
  - `ApiDefinition.Connection` -> `ApiConnection.ApiDefinitions` (FK `ConnectionId`, required)
  - `ApiParameter.ApiDefinition` -> `ApiDefinition.Parameters` (FK, cascade delete)
  - `ApiKey.ApiDefinition` -> `ApiDefinition.ApiKeys` (FK, cascade delete)
- Unique index: `ApiDefinition.Route`
- One migration: `20261101003458_Initial`

Important startup behavior:
- `Program.cs` calls `db.Database.Migrate()` inside try/catch.
- Any migration failure is logged to console and ignored; app continues.

## 8. UI / Controller Behavior

### `ConnectionsController`

Standard MVC CRUD:
- `Index`: list connections
- `Create` (GET/POST)
- `Edit` (GET/POST)
- `Delete` (POST, deletes by id; cascades to ApiDefinitions per DB FK)

### `ApisController`

- `Index`: list APIs including connection
- `Create` (GET/POST): connection dropdown, command type enum dropdown, command text, description
- `Details`: shows parameters and API keys; forms to add/delete parameters and keys; generate per-API or global keys
- `Edit` (GET/POST): updates API; note: EF `Update` replaces aggregate, and parameters/keys are not edited here
- `Delete` (POST): deletes API (cascades params/keys)
- `AddParameter`, `DeleteParameter`, `AddKey`, `DeleteKey`, `AddGlobalKey`

`AddKey`:
- Creates an `ApiKey` with `ApiDefinitionId = apiId`.
- Shows generated key once via `TempData["NewKey"]`.

`AddGlobalKey`:
- Creates `ApiKey` with `ApiDefinitionId = null`.
- Also uses `TempData["NewKey"]`.

### `HomeController`

- `Index`, `Privacy`, `Error` (standard template).

### Views

Bootstrap 5 UI, navbar links to Home, Connections, APIs. `_Layout.cshtml` includes jQuery, Bootstrap bundle, site JS/CSS.

## 9. API Gateway Runtime

Route:
```csharp
[ApiController]
[Route("api/{*route}")]
public class ApiGatewayController : ControllerBase
```
- Handles `GET`, `POST`, `PUT`, `DELETE` via `[HttpGet, HttpPost, HttpPut, HttpDelete]`.
- Catch-all `route` parameter is matched against `ApiDefinition.Route`.
- No auth beyond API key check.

Lookup:
```csharp
var api = await _db.ApiDefinitions
    .Include(a => a.Connection)
    .Include(a => a.Parameters)
    .Include(a => a.ApiKeys)
    .FirstOrDefaultAsync(a => a.Route == route, ct);
```
If no API: `404 { error: "API not found" }`.

API key validation:
- Reads `X-API-Key` header or `api_key` query string.
- Loads active API keys where `ApiDefinitionId == null` OR `ApiDefinitionId == api.Id`.
- If no configured keys exist for this API: `401 { error: "No API keys configured for this API." }`.
- If key missing/invalid: `401 { error: "Invalid or missing API key." }`.

Input gathering:
- Query parameters are added, except `api_key`.
- If request body content length > 0 and `Content-Type` contains `json`, parses JSON object properties and adds them to input dictionary.
- JSON values are stored as `JsonElement` (cloned).

Execution:
- Calls `ApiExecutor.ExecuteAsync(api, input)`.
- Success: `200 { resultSets, outputParameters }`.
- Failure: `400 { error: exception.Message }`.

## 10. ApiExecutor Internals

File: `Services/ApiExecutor.cs`, registered as scoped service.

### Connection creation

```csharp
DbProvider.MsSql   -> SqlConnection
DbProvider.MySql   -> MySqlConnection
DbProvider.Postgres -> NpgsqlConnection
```

### Command setup

- `CommandText = api.CommandText`
- `CommandType`:
  - `StoredProcedure` -> `CommandType.StoredProcedure`
  - `Query` -> `CommandType.Text`
- Parameters are created via `cmd.CreateParameter()` and configured:
  - Parameter name normalized:
    - Postgres: name as-is
    - SQL Server/MySQL: `"@" + name.TrimStart("@")`
  - Direction input/output
  - `DbType` mapped
  - Value handling for input:
    - If input contains matching value and is not null: convert
    - Else if default value present: convert default
    - Else if not required: `DBNull.Value`
    - Else throw `ArgumentException("Missing required parameter '{name}'.")`

### Type mapping

`MapDbType`:
- int/integer -> Int32
- long/bigint -> Int64
- double/float -> Double
- decimal -> Decimal
- bool/boolean -> Boolean
- datetime/date -> DateTime
- guid -> Guid
- otherwise -> String

`ConvertValue`:
- If `JsonElement`, extracts typed value using corresponding `Get*` method; strings become `je.ToString()`.
- If other type, converts from `ToString()` using Parse methods.
- Date parse uses `DateTime.Parse`; decimal parse `decimal.Parse` (culture-sensitive default).

### Execution and results

- Uses `cmd.ExecuteReaderAsync()`.
- Loops `do { ... } while (reader.NextResultAsync())`.
- For each result set:
  - captures column names
  - reads rows into `List<Dictionary<string, object?>>` (case-insensitive keys)
  - DBNull becomes null
- After reader disposed:
  - For each declared output parameter, reads `dbp.Value` and sets `null` if DBNull.

Return:
```csharp
public class ExecutionResult
{
    public List<List<Dictionary<string, object?>>> ResultSets;
    public Dictionary<string, object?> OutputParameters;
}
```

### Caveats/limitations

- Output parameter values for SQL Server stored procedures may only be available after all result sets are consumed; code reads them after reader closes, which is correct.
- OUTPUT parameters are not added separately beyond declared parameters; both in/out use the same parameter name and direction.
- No transaction or cancellation passed to DB command beyond query CT for gateway? `ExecuteAsync` signature only takes `CancellationToken`? Actually `ExecuteAsync(ApiDefinition api, Dictionary input)` has no CT. Gateway does not pass CT to executor. This is a minor gap.
- If a stored procedure has no result sets, reader still works? `FieldCount` 0; loops yield one empty table? Note `ReadAsync` returns false; still adds empty list. Then NextResult false. So response includes an empty result set.
- No query timeout configuration; uses provider defaults.
- No SQL injection protection for `CommandText` Query type; command text is fully trusted/admin-defined.
- Exceptions are returned to caller as 400 with full message. Avoid exposing production DB errors.

## 11. Supported Providers and Parameter Rules

- MsSql: parameter names prefixed with `@`.
- MySql: parameter names prefixed with `@`.
- Postgres: parameter names not prefixed (Npgsql named parameters).
- Postgres stored procedure/command text behavior depends on how `CommandText` is authored; executor uses Npgsql named parameters.

DbType strings accepted:
- int/integer
- long/bigint
- double/float
- decimal
- bool/boolean
- datetime/date
- guid
- anything else -> string

## 12. Key Files

| File | Purpose |
| --- | --- |
| `Program.cs` | app builder, DbContext registration, scoped `ApiExecutor`, middleware, auto-migrate, routing |
| `Controllers/ApiGatewayController.cs` | catch-all API invocation route, API key validation, input parsing, execution call |
| `Controllers/ConnectionsController.cs` | MVC CRUD for DB connections |
| `Controllers/ApisController.cs` | MVC management for APIs, parameters, API keys |
| `Data/AppDbContext.cs` | EF model and relationships |
| `Services/ApiExecutor.cs` | multi-provider SQL execution and result serialization |
| `Models/ApiConnection.cs` | entities and enums |
| `Views/Apis/Details.cshtml` | parameter and key management UI |
| `Data/Migrations/20261101003458_Initial.cs` | initial PostgreSQL schema |
| `appsettings.json` | metadata DB connection |

## 13. Capability-to-File Map

| Capability | Primary Implementation | UI / Related Files |
| --- | --- | --- |
| App startup / DB migration / routing | `Program.cs` | `appsettings.json` |
| Connection CRUD | `Controllers/ConnectionsController.cs` | `Views/Connections/*` |
| API definition CRUD | `Controllers/ApisController.cs` | `Views/Apis/*` |
| Parameter management | `ApisController.AddParameter/DeleteParameter` | `Views/Apis/Details.cshtml` |
| API key management (per-API/global) | `ApisController.AddKey/AddGlobalKey/DeleteKey` | `Views/Apis/Details.cshtml`, `Views/Apis/Index.cshtml` |
| Runtime API invocation | `Controllers/ApiGatewayController.cs` | `Services/ApiExecutor.cs` |
| SQL execution/result mapping | `Services/ApiExecutor.cs` | `Models/ApiConnection.cs` |
| EF schema | `Data/AppDbContext.cs`, `Data/Migrations/*` | database |
| Layout/static assets | `Views/Shared/_Layout.cshtml`, `wwwroot/*` | all views |

## 14. Development Notes

- Target framework is .NET 10; SDK 10 required.
- `ImplicitUsings` and nullable reference types are enabled.
- Auto-migration is best-effort; if it fails, app still runs but API calls fail at runtime until DB is fixed.
- Build artifacts may lock executables; stop running process before rebuild.
- No tests are present.
- There is no `.git` repository in `BuildWorkflowAPI` workspace at current location (parent workspace may differ).
- If adding features:
  - Consider enforcing `HttpMethod` in gateway (currently ignored).
  - Consider authentication/authorization before exposing externally.
  - Consider encrypting stored connection strings/API keys.
  - Consider adding cancellation token plumbing through `ApiExecutor`.
  - Consider query timeout and parameterized safety for `Query` command type (already parameterized for declared params, but command text itself is trusted).

## 15. Example Invocation

Assuming an API with route `get-users`, a parameter `id`, and key `abc123`:

```bash
curl -X POST http://localhost:5268/api/get-users \
  -H "X-API-Key: abc123" \
  -H "Content-Type: application/json" \
  -d '{"id": 42}'
```

Or with query key:

```bash
curl "http://localhost:5268/api/get-users?api_key=abc123&id=42"
```

Expected success response:

```json
{
  "resultSets": [
    [
      { "id": 42, "name": "..." }
    ]
  ],
  "outputParameters": {}
}
```

## 16. Entity Relationship Summary

```
ApiConnection 1---* ApiDefinition
ApiDefinition 1---* ApiParameter (cascade delete)
ApiDefinition 1---* ApiKey (cascade delete)
ApiKey *---1 ApiDefinition (optional; null = global key)
```

`ApiDefinition.Route` is unique. Deleting a connection cascades to its APIs, then to parameters/keys. Deleting an API cascades to its parameters/keys.

## 17. Current Gaps / Future Work

- No login/auth even though `AddAuthorization` is called.
- No edit UI for parameters in bulk or API command key rotation.
- No request/response logging or audit trail.
- No pagination on Connections/APIs lists.
- No OpenAPI/Swagger configuration.
- Error responses include raw exception messages.
- Database for app metadata and any target DB both use PostgreSQL default provider settings; target DB provider is configurable only per connection.
- Output parameters for non-result-set stored procedures are still returned after reader closes.

## 18. Tracker Maintenance Rule
    Update this file when changing:
    - project purpose or stack
    - database schema/entities
    - controller routes or API contract
    - `ApiExecutor` behavior
    - configuration keys
    - major UI workflows
    This keeps future AI models and developers aligned with the actual implementation.

## 19. Recent Updates (AI)
- Vulnerabilities fixed:
  - Updated NuGet packages (`MySqlConnector` 2.6.2, `Microsoft.Data.SqlClient` 7.1.1, Npgsql EF 10.0.3, EF Design 10.0.14, explicit `System.Security.Cryptography.Pkcs` 10.0.14).
  - Gateway no longer leaks raw DB error messages.
- Authentication/admin protection:
  - Added `AppUser` table (`AppUsers`), cookie authentication, `AccountController` (login/logout only), protected Connections/APIs controllers. Register page removed.
  - Seeded app admin user (`admin` / `Password123!`) via `sql/seed-app-users.postgres.sql`.
- SSO:
  - Added `SsoController` flow (token -> validation API -> create/login user) and `Sso:ValidationUrl` setting.
- Logging:
  - Added `ApiCallLog` table and gateway writes request/response/status/key/timestamp.
  - Added protected `LogsController` and `/Logs` view; nav link.
- UI:
  - Google Console-inspired layout/theme; side nav, app bar; bullets removed.
- Sample API:
  - Added app-db sample (sample_employees table + get_sample_employees) and API definition `/api/app-sample-employees`.
  - SQL seeders in `sql/`: app users, sample APIs (mssql/postgres/appdb).
- Notes:
  - App metadata DB can contain pre-existing `Users` table from another project; auth uses `AppUsers` to avoid collision.

Update this file when changing:
- project purpose or stack
- database schema/entities
- controller routes or API contract
- `ApiExecutor` behavior
- configuration keys
- major UI workflows

This keeps future AI models and developers aligned with the actual implementation.
