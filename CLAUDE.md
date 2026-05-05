# CLAUDE.md

This file provides guidance to Claude Code/Codex when working in the `flex-auth-service` repository.

## Project Overview

`flex-auth-service` is a .NET 9 / ASP.NET Core Flex.Auth service responsible for authentication, JWT issuance, and basic user management.

The service currently focuses on:

- Username/password login and JWT issuance.
- Basic user management.
- Identity persistence with Entity Framework Core, Oracle, and Oracle Wallet-based connectivity.
- Reliable integration event handling with the Transactional Outbox pattern and RabbitMQ publishing.
- Infrastructure-level concerns such as structured logging, correlation ID propagation, exception handling, request guarding, rate limiting, and forwarded headers.

## Solution Structure

The main solution is:

```powershell
Flex.Auth.sln
```

Current projects:

| Project | Path | Purpose |
|---|---|---|
| `Flex.Auth` | `src/Flex.Auth/Flex.Auth.csproj` | ASP.NET Core Web API host for the Flex.Auth service. The project file is still named `Flex.Auth.csproj` in the current repository. |
| `Flex.Domain` | `src/Flex.Domain/Flex.Domain.csproj` | Domain entities, domain events, constants, and base abstractions |
| `Flex.Infrastructures` | `src/Flex.Infrastructures/Flex.Infrastructures.csproj` | Cross-cutting infrastructure: EF Core/Oracle, JWT, RabbitMQ, Outbox/Inbox, logging, middleware, response handling, resilience, OpenAPI, and rate limiting |

Important directories:

| Directory | Purpose |
|---|---|
| `src/Flex.Auth/Controllers` | HTTP API controllers such as `AuthController` and `UsersController` |
| `src/Flex.Auth/Services` | Application/business logic for authentication and user operations |
| `src/Flex.Auth/Repositories` | EF Core-backed repositories used by the application layer |
| `src/Flex.Auth/Events` | Application-level event routing, including RabbitMQ routing keys |
| `src/Flex.Domain/Entities` | Domain entities such as `User`, `Role`, `Permission`, `LoginHistory`, `OutboxMessage`, and `InboxMessage` |
| `src/Flex.Infrastructures/Persistence` | `IdentityDbContext`, EF Core entity configurations, converters, and seed code |
| `src/Flex.Infrastructures/Messaging` | RabbitMQ integration plus Transactional Outbox and Inbox infrastructure |
| `src/Flex.Infrastructures/Observability` | Correlation ID propagation and global request/response logging |
| `secrets/oracle-wallet` | Local Oracle Wallet files; do not copy real secrets into docs, logs, or examples |
| `docs` | Technical documentation; some files may describe planned designs rather than implemented behavior |

## Build Commands

Run commands from the repository root:

```powershell
# Restore dependencies
dotnet restore Flex.Auth.sln

# Build the entire solution
dotnet build Flex.Auth.sln

# Build the API host project only
dotnet build src/Flex.Auth/Flex.Auth.csproj

# Run the Flex.Auth API locally
dotnet run --project src/Flex.Auth/Flex.Auth.csproj
```

Local launch profiles are defined in `src/Flex.Auth/Properties/launchSettings.json`:

```text
HTTP  : http://localhost:5050
HTTPS : https://localhost:7040
```

To run with the development environment in PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Flex.Auth/Flex.Auth.csproj
```

## Runtime Configuration

Configuration is loaded in `src/Flex.Auth/Extensions/HostExtensions.cs` in this order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables

There is no separate `Config/` directory, and this project does not use `ROOT_FOLDER` for configuration loading.

Main configuration sections:

| Section | Purpose |
|---|---|
| `OracleWallet` | Oracle Wallet connection settings: `User`, `Password`, `DataSource`, and `WalletPath` |
| `JwtSettings` | JWT signing and validation settings such as `SecretKey`, `Issuer`, `Audience`, algorithm, and token lifetime |
| `RabbitMQ` | RabbitMQ publisher settings and exchange name used by the outbox publisher |
| `RabbitMQConsumer` | RabbitMQ consumer settings, used when inbox/consumer infrastructure is enabled |
| `Elastic` | Serilog sink settings for Elasticsearch/OpenSearch; can be disabled with `Enabled=false` |
| `Resilience` | Downstream HTTP timeout, retry, and circuit breaker settings |
| `TrustedIPs` | Trusted proxy IP configuration for forwarded headers |

Security note: `src/Flex.Auth/appsettings.json` currently contains real credentials for Oracle, RabbitMQ, Elastic, and JWT. Do not copy those values into documentation, logs, examples, or generated output. Prefer environment variables or a secret manager for overrides, for example:

```powershell
$env:JwtSettings__SecretKey = "<secret>"
$env:OracleWallet__Password = "<secret>"
```

## Key Patterns

### Entity Creation Flow

When adding a new persisted entity, follow the current EF Core/Oracle structure:

1. **Entity** - Create the entity in `src/Flex.Domain/Entities`. Use the existing domain base type only when it fits the model, for example `EntityBase<TKey>`.
2. **DbSet** - Add a `DbSet<TEntity>` to `IdentityDbContext` in `src/Flex.Infrastructures/Persistence/IdentityDbContext.cs`.
3. **EF configuration** - Add an `IEntityTypeConfiguration<TEntity>` implementation under `src/Flex.Infrastructures/Persistence/Configurations`.
4. **Oracle mapping** - Map table and column names explicitly. Existing tables use uppercase Oracle names, max-length constraints, indexes, and converters such as `BoolToCharConverter` where needed.
5. **Repository interface** - Add an interface under the relevant `Repositories/Interfaces` folder only if application logic needs data access beyond direct DbContext usage.
6. **Repository implementation** - Implement repository methods with EF Core async APIs. Use `AsNoTracking()` for read-only queries.
7. **Service interface and implementation** - Add application/business logic under `Services/Interfaces` and `Services` when the entity has behavior or workflows.
8. **Controller or consumer** - Add an API controller or message handler only if the entity needs an external entry point.
9. **DI registration** - Register new repositories and services in `AddApplicationServices()` or the most appropriate infrastructure extension.

Use constructor injection. Do not copy the old `IServiceProvider` lazy-resolution pattern from the previous monorepo documentation.

### Naming Conventions

Use the existing naming style in the repository instead of conventions from the old monorepo.

| Type | Convention | Example |
|---|---|---|
| Namespace | Use the Auth service namespace consistently. New code should use `Flex.Auth`, `Flex.Domain`, or `Flex.Infrastructures` as appropriate. | `Flex.Auth.Services` |
| Entity | Use clear singular domain names without an `Entity` suffix unless an existing pattern requires it | `User`, `Role`, `LoginHistory`, `OutboxMessage` |
| Entity base type | Use `EntityBase<TKey>` only when the entity needs the shared domain base fields | `User : EntityBase<long>` |
| EF configuration | Use `{EntityName}Configuration` and implement `IEntityTypeConfiguration<TEntity>` | `UserConfiguration` |
| Repository interface | Use `I{Name}Repository` | `IUserRepository` |
| Repository implementation | Use `{Name}Repository` | `UserRepository` |
| Service interface | Use `I{Name}Service` | `IAuthService`, `IUserService` |
| Service implementation | Use `{Name}Service` | `AuthService`, `UserService` |
| Controller | Use plural resource names when the API represents a collection; keep ASP.NET Core `Controller` suffix | `UsersController`, `AuthController` |
| Request/command model | Use action-oriented names for inputs | `LoginRequest`, `CreateUserCommand` |
| Response model | Use `{Name}Response`, `{Name}Result`, or a similarly explicit output name | `UserResponse`, `LoginResult` |
| Options/settings | Use `{Feature}Options` or `{Feature}Settings` and bind from the matching config section | `RabbitMQOptions`, `JwtSettings` |
| Domain event | Use past-tense business event names | `UserLoginAttemptedEvent` |
| Event routing | Keep routing keys explicit and centralized in the resolver | `EventRoutingResolver.RoutingKeys.UserLogin` |

Oracle persistence conventions:

- Map table and column names explicitly in EF Core configuration.
- Existing Oracle table and column names are uppercase, for example `USERS`, `NORMALIZED_USER_NAME`, and `OUTBOX_MESSAGES`.
- Use max-length constraints and indexes consistently with the existing configuration files.
- Use converters such as `BoolToCharConverter` for Oracle `CHAR(1)` boolean columns.
- Keep read-only EF queries `AsNoTracking()` unless the entity must be updated in the same unit of work.

### Infrastructure

- **Database**: Oracle via Entity Framework Core and Oracle Wallet.
- **Authentication**: JWT Bearer authentication with custom `JwtSettings`, token generation, and authorization policies.
- **Messaging**: RabbitMQ publisher/consumer infrastructure with Transactional Outbox and Inbox support.
- **Logging**: Serilog with structured logging and optional Elasticsearch/OpenSearch sink.
- **Observability**: Correlation ID propagation through `X-Correlation-Id` and global request/response logging.
- **Resilience**: Downstream HTTP timeout, retry, circuit breaker, and bulkhead policies via Polly/resilience extensions.
- **Rate limiting**: ASP.NET Core global rate limiting with token bucket and concurrency limiter policies.
- **OpenAPI**: Swagger/OpenAPI configuration with lowercase document filtering.
- **HTTP pipeline**: Forwarded headers, request guard middleware, centralized exception handling, CORS, routing conventions, authentication, authorization, and controller mapping.
- **Response model**: Standard API response wrapper through `Result` and shared response/error codes.

### SQL Safety

- Prefer LINQ and EF Core query APIs over raw SQL.
- If raw SQL is required, use `FromSqlInterpolated`, `ExecuteSqlInterpolated`, or explicit `DbParameter` instances. Do not concatenate user input into SQL strings.
- Treat Oracle table and column names as fixed schema identifiers. Do not build identifiers dynamically from request data.
- Validate and whitelist any dynamic sorting, filtering, or column selection before applying it to a query.
- Keep read-only queries `AsNoTracking()` unless entity tracking is required.
- Do not log SQL statements with secrets, passwords, tokens, connection strings, or personally sensitive payloads.

### Responses, Errors, and Validation

- Use `Flex.Infrastructures.Responses.Result` as the standard API response wrapper.
- Return successful controller responses with `Result.Success(...)`.
- Do not build ad-hoc response shapes for normal API responses unless a specific endpoint contract requires it.
- Use `Flex.Infrastructures.Exceptions.ValidationException` with `ResponseCode` for business and validation failures.
- Keep validation and business rule checks in services, not in repositories.
- Let `ExceptionHandlingMiddleware` translate exceptions into consistent HTTP responses.
- `ValidationException` maps to `400 Bad Request`.
- JWT/security validation failures map to `401 Unauthorized` or `403 Forbidden` depending on the exception.
- Polly timeout and circuit breaker failures map to gateway/service-unavailable style responses.
- `RequestGuardMiddleware` rejects `POST`, `PUT`, and `PATCH` requests with a body when `Content-Type` is missing or not `application/json`.
- Do not expose sensitive details in error messages, especially credentials, tokens, connection strings, Oracle Wallet paths, or raw exception internals.

### Logging and Observability

- Logging is based on Serilog and configured through `Flex.Infrastructures.Logging.SeriLogger`.
- Use structured logging with named properties instead of string-concatenated log messages.
- `CorrelationIdMiddleware` reads or creates `X-Correlation-Id`, writes it back to the response, and pushes it into the Serilog log context.
- `GlobalLoggingMiddleware` records request/response metadata such as method, path, status code, duration, user/client identifiers, IP address, and user agent.
- Request and response body logging must remain controlled by configuration and path whitelisting.
- Do not log passwords, JWTs, refresh tokens, Oracle Wallet details, connection strings, credentials, or sensitive personal data.
- Keep business audit data separate from technical logs. Use domain events/outbox records for business-significant events when reliability matters.
- Preserve correlation ID propagation when adding downstream HTTP calls or message publishing.
- Elasticsearch/OpenSearch logging is optional and controlled by the `Elastic` configuration section.

## GitNexus

GitNexus is not currently configured for this repository.

- There is no `.gitnexus/` directory at the repository root.
- Do not assume the old `FLEX.Auth.Backend` GitNexus index applies to this project.
- Do not require `gitnexus_impact`, `gitnexus_detect_changes`, or `npx gitnexus analyze` as mandatory steps unless a valid GitNexus index and tools are added to this repository.
- Use normal code navigation instead: `rg`, solution/project references, EF Core mappings, and direct call-site inspection.
- If GitNexus is added later, update this section with the real repository name, available tools, index location, and required workflow.

## Code Conventions

- Target framework is `net9.0`; nullable reference types and implicit usings are enabled.
- Keep controllers thin. Controllers should handle HTTP concerns, delegate business logic to services, and return `Result` responses.
- Keep service methods focused on business workflows, validation, orchestration, and transaction boundaries.
- Keep repositories focused on persistence and EF Core queries. Do not put business decisions in repositories.
- Use async EF Core APIs for database operations.
- Pass `CancellationToken` through new public async paths when the token can come from a controller action or background service.
- Use constructor injection for dependencies. Do not use service locator patterns for normal application code.
- Avoid adding new NuGet packages when existing infrastructure or BCL APIs are sufficient.
- Prefer small, explicit models over generic dictionaries or anonymous response shapes for public API contracts.
- Do not introduce cross-layer shortcuts. `Flex.Domain` should not depend on infrastructure or API projects.
- Keep cross-cutting behavior in `Flex.Infrastructures`; keep Flex.Auth-specific workflows in the Auth service project.

## Authentication and Token Flow

Authentication is implemented with JWT Bearer authentication and the login workflow in `AuthService`.

JWT setup:

- JWT options are bound from the `JwtSettings` configuration section.
- Token validation must keep issuer, audience, lifetime, and signing key validation enabled.
- `MapInboundClaims = false` is intentional; use claim names from `Flex.Infrastructures.Authentication.ClaimTypes`.
- `[AllowAnonymous]` endpoints are allowed to bypass bearer token validation.
- `TokenService.GenerateToken()` creates tokens from `JwtSettings` and explicit claims.

Login flow:

1. `AuthService.LoginAsync()` reads request context such as client IP through `IRequestContextAccessor`.
2. The user is loaded by normalized username through `IUserRepository`.
3. Passwords are verified with `IPasswordHasher<User>`.
4. Failed and successful login attempts both create `UserLoginAttemptedEvent` records through `IOutboxWriter`.
5. `IdentityDbContext.SaveChangesAsync()` persists the outbox event before returning the token.
6. The issued JWT currently includes claims such as `jti`, `iss`, `aud`, `sub`, and `email`.

Security rules:

- Do not reveal whether the username or password was wrong. Use `ValidationException(ResponseCode.InvalidCredentials)`.
- Do not log raw passwords, password hashes, JWTs, signing keys, or authentication headers.
- Keep token claims minimal and avoid adding sensitive profile data unless required by downstream authorization.
- If adding refresh token, logout, blacklist, MFA, SSO, or tenant-selection flows, update this section and the API documentation together.

## Documentation Maintenance

Keep `CLAUDE.md` aligned with the actual codebase.

- When changing architecture, project structure, infrastructure wiring, configuration, authentication flow, persistence patterns, messaging, logging, error handling, or build/deployment behavior, update this file in the same change.
- If implementing a new pattern that is not covered here, add a concise guideline so future agents follow it consistently.
- If removing or replacing an existing pattern documented here, update or delete the outdated guidance.
- Do not leave instructions that reference removed files, old project names, unavailable tools, or obsolete workflows.
- Treat this file as the source of working guidance for agents, not as historical documentation.
