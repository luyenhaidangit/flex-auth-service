# Spec: flex-auth-service — Missing Features & Improvements

## Objective

Hoàn thiện `flex-auth-service` cho production bằng cách implement các tính năng còn thiếu đã được thiết kế nhưng chưa code, và cải thiện chất lượng codebase với validation và test coverage.

**Target users:** Backend developers và downstream services (API Gateway, microservices) sử dụng auth service như identity provider.

**Success conditions:**
- Client có thể refresh JWT mà không cần login lại khi access token hết hạn.
- Client có thể revoke token (logout) và token bị revoke không thể dùng tiếp.
- Client đã authenticate có thể lấy thông tin user hiện tại qua `/auth/me`.
- Mọi DTO đầu vào đều có validation rõ ràng với error message chuẩn.
- RBAC enforcement thực sự hoạt động dựa trên Permission entity có sẵn.
- Unit test và integration test bao phủ các luồng chính.

---

## Tech Stack

| Thành phần | Version | Ghi chú |
|---|---|---|
| Runtime | .NET 9.0 | net9.0, nullable enabled, implicit usings |
| Framework | ASP.NET Core Web API | Controllers thin, business logic ở Services |
| Database | Oracle via EF Core | Oracle.EntityFrameworkCore 9.23.26000, uppercase table/column names |
| ORM | Entity Framework Core 9 | Async APIs, AsNoTracking() cho read-only |
| Identity | ASP.NET Core Identity | PasswordHasher, UserToken entity sẵn có |
| Auth | JWT Bearer (HMAC-SHA256) | JwtSettings, TokenService có sẵn |
| Validation | FluentValidation 12 | Đã có package, chưa có validator nào |
| Messaging | RabbitMQ + Outbox/Inbox | Reliable event delivery cho audit events |
| Logging | Serilog + ECS | Structured logging, correlation ID |
| Resilience | Polly 8 | Timeout, retry, circuit breaker |
| Testing | xUnit + FluentAssertions | Sẽ thêm vào solution |

**Không thêm Redis.** Token blacklist và refresh token đều lưu Oracle DB để tránh dependency mới. TTL cleanup bằng background job hoặc EF query lọc theo expiry.

---

## Commands

```powershell
# Restore
dotnet restore Flex.Auth.sln

# Build toàn bộ
dotnet build Flex.Auth.sln

# Chạy local (development)
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Flex.Auth/Flex.Auth.csproj

# Chạy tests
dotnet test Flex.Auth.sln

# Chạy tests với coverage
dotnet test Flex.Auth.sln --collect:"XPlat Code Coverage"

# Lint (nếu dotnet-format đã cài)
dotnet format Flex.Auth.sln --verify-no-changes
```

URLs local:
```
HTTP  : http://localhost:5050
HTTPS : https://localhost:7040
Swagger: https://localhost:7040/swagger/ui (Development only)
```

---

## Project Structure

Thêm mới so với hiện tại:

```
flex-auth-service/
├── src/
│   ├── Flex.Auth/
│   │   ├── Controllers/
│   │   │   └── AuthController.cs          # Thêm: /refresh-token, /logout, /me endpoints
│   │   ├── Services/
│   │   │   ├── AuthService.cs             # Thêm: RefreshTokenAsync, LogoutAsync, GetCurrentUserInfoAsync
│   │   │   ├── Interfaces/
│   │   │   │   └── IAuthService.cs        # Thêm method signatures mới
│   │   │   ├── PermissionService.cs       # Mới: RBAC enforcement service
│   │   │   └── Interfaces/
│   │   │       └── IPermissionService.cs  # Mới
│   │   ├── Models/Auth/                   # Mới: thư mục tách DTOs auth
│   │   │   ├── RefreshTokenRequest.cs     # Mới
│   │   │   ├── RefreshTokenResult.cs      # Mới
│   │   │   ├── LogoutRequest.cs           # Mới
│   │   │   └── UserInfoResult.cs          # Mới (đang comment out)
│   │   ├── Models/Users/
│   │   │   └── (giữ nguyên)
│   │   ├── Validators/                    # Mới: FluentValidation validators
│   │   │   ├── LoginRequestValidator.cs
│   │   │   ├── CreateUserCommandValidator.cs
│   │   │   └── RefreshTokenRequestValidator.cs
│   │   └── Repositories/
│   │       ├── RefreshTokenRepository.cs  # Mới
│   │       └── Interfaces/
│   │           └── IRefreshTokenRepository.cs  # Mới
│   │
│   ├── Flex.Domain/
│   │   └── Entities/
│   │       ├── RefreshToken.cs            # Mới: entity lưu refresh token
│   │       └── RevokedToken.cs            # Mới: entity lưu token bị revoke (blacklist)
│   │
│   └── Flex.Infrastructures/
│       ├── Persistence/
│       │   ├── IdentityDbContext.cs       # Thêm: DbSet<RefreshToken>, DbSet<RevokedToken>
│       │   └── Configurations/
│       │       ├── RefreshTokenConfiguration.cs   # Mới
│       │       └── RevokedTokenConfiguration.cs   # Mới
│       └── Authentication/
│           ├── ITokenBlacklistService.cs  # Mới: interface check revoked token
│           └── TokenBlacklistService.cs   # Mới: Oracle-backed implementation
│
└── tests/
    ├── Flex.Auth.UnitTests/               # Mới: xUnit project
    │   ├── Flex.Auth.UnitTests.csproj
    │   ├── Services/
    │   │   ├── AuthServiceTests.cs
    │   │   └── UserServiceTests.cs
    │   └── Validators/
    │       ├── LoginRequestValidatorTests.cs
    │       └── CreateUserCommandValidatorTests.cs
    └── Flex.Auth.IntegrationTests/        # Mới: xUnit project
        ├── Flex.Auth.IntegrationTests.csproj
        └── Controllers/
            ├── AuthControllerTests.cs
            └── UsersControllerTests.cs
```

---

## Code Style

Theo quy ước đang có trong repo. Ví dụ code mới phải viết theo pattern này:

```csharp
// Service: business logic thuần túy, inject dependencies qua constructor
public class AuthService : IAuthService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(IdentityDbContext dbContext, IRefreshTokenRepository refreshTokenRepository)
    {
        _dbContext = dbContext;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<RefreshTokenResult> RefreshTokenAsync(
        RefreshTokenRequest request, CancellationToken ct = default)
    {
        // Validate → Business logic → Persist → Return result
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new ValidationException(ResponseCode.InvalidToken);

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new ValidationException(ResponseCode.TokenExpired);

        // ... tiếp tục
        await _dbContext.SaveChangesAsync(ct);
        return new RefreshTokenResult(newAccessToken, newRefreshToken);
    }
}

// Controller: thin, chỉ handle HTTP concerns
[HttpPost("refresh-token")]
[AllowAnonymous]
public async Task<IActionResult> RefreshToken(
    [FromBody] RefreshTokenRequest request, CancellationToken ct)
{
    var result = await _authService.RefreshTokenAsync(request, ct);
    return Ok(Result.Success(result));
}

// Validator: khai báo rules tập trung
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

// Entity mới: dùng EntityBase<TKey> khi cần domain base fields
public class RefreshToken : EntityBase<long>
{
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}

// EF config: map Oracle UPPERCASE, explicit constraints
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("REFRESH_TOKENS");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasColumnName("TOKEN").HasMaxLength(512).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("EXPIRES_AT").IsRequired();
        builder.Property(x => x.IsRevoked).HasColumnName("IS_REVOKED")
               .HasConversion<BoolToCharConverter>().HasMaxLength(1);
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
```

**Naming:**
- Entity: `RefreshToken`, `RevokedToken` (không suffix `Entity`)
- Interface: `IRefreshTokenRepository`, `IPermissionService`, `ITokenBlacklistService`
- Validator: `LoginRequestValidator` (AbstractValidator<T>)
- Domain event: past tense — `UserLoggedOutEvent`

**Không làm:**
- Không log raw token, refresh token, password, JWT value
- Không expose chi tiết lỗi khác nhau cho username vs password sai (dùng `ResponseCode.InvalidCredentials`)
- Không đặt business logic trong Repository
- Không dùng service locator pattern

---

## Implementation Plan

### Feature 1: Refresh Token

**Entities cần tạo:**
```
RefreshToken { Id, UserId, Token (SHA-256 hash), ExpiresAt, IsRevoked, CreatedAt }
```

Dùng `UserToken` entity có sẵn trong schema **nếu** fit — nếu không (cần `IsRevoked`, `ExpiresAt` riêng), tạo entity `RefreshToken` mới.

**Flow:**
```
POST /api/auth/login
  → tạo access token (JWT, 60 phút)
  → tạo refresh token (random secure string, 7 ngày)
  → lưu hash của refresh token vào REFRESH_TOKENS
  → trả { accessToken, refreshToken }

POST /api/auth/refresh-token { refreshToken }
  → lookup hash trong DB
  → validate: tồn tại, chưa revoke, chưa hết hạn, userId khớp
  → revoke token cũ (IsRevoked = true)
  → tạo access token + refresh token mới (rotation)
  → trả { accessToken, refreshToken }
```

**Endpoint:**
```
POST /api/auth/refresh-token
Body: { "refreshToken": "..." }
Response: { accessToken, refreshToken }
Auth: [AllowAnonymous]
```

---

### Feature 2: Logout + Token Blacklist

**Entity cần tạo:**
```
RevokedToken { Id, Jti (unique), RevokedAt, ExpiresAt }
```

Cleanup: query lọc `ExpiresAt < now` trong `OutboxProcessor` hoặc background job riêng.

**Flow:**
```
POST /api/auth/logout
  → extract jti + exp từ ClaimsPrincipal (user đang login)
  → nếu token chưa hết hạn → insert vào REVOKED_TOKENS
  → revoke refresh token liên quan (nếu có)
  → publish UserLoggedOutEvent qua Outbox
  → trả 200 OK

JWT validation middleware:
  → sau khi validate chữ ký → check jti trong REVOKED_TOKENS
  → nếu bị revoke → trả 401
```

**Tích hợp vào pipeline:**
Thêm `ITokenBlacklistService` vào JWT validation event `OnTokenValidated`:
```csharp
options.Events = new JwtBearerEvents
{
    OnTokenValidated = async ctx =>
    {
        var blacklist = ctx.HttpContext.RequestServices
            .GetRequiredService<ITokenBlacklistService>();
        var jti = ctx.Principal?.FindFirstValue(ClaimTypes.Jti);
        if (jti != null && await blacklist.IsRevokedAsync(jti))
            ctx.Fail("Token has been revoked.");
    }
};
```

**Endpoint:**
```
POST /api/auth/logout
Body: {} (empty, dùng token từ Authorization header)
Auth: [Authorize]
```

---

### Feature 3: GET /auth/me

**Flow:**
```
GET /api/auth/me
  → extract sub (username) từ ClaimsPrincipal
  → query UserRepository
  → trả UserInfoResult { userName, email, fullName, roles }
```

**Endpoint:**
```
GET /api/auth/me
Auth: [Authorize]
Response: { userName, email, fullName, roles[] }
```

Uncomment và hoàn thiện `GetCurrentUserInfoAsync` đang bị comment trong `AuthService.cs`.

---

### Feature 4: FluentValidation

Đăng ký validators qua DI:
```csharp
services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
```

Validator cho từng DTO:

| DTO | Rules |
|---|---|
| `LoginRequest` | UserName: NotEmpty, MaxLength(256); Password: NotEmpty, MaxLength(128) |
| `CreateUserCommand` | UserName: NotEmpty, MaxLength(256); Email: NotEmpty, EmailAddress, MaxLength(256); FullName: NotEmpty, MaxLength(512) |
| `RefreshTokenRequest` | RefreshToken: NotEmpty |

Validation errors throw `ValidationException` (existing) → `ExceptionHandlingMiddleware` map sang 400.

---

### Feature 5: RBAC Enforcement

**Hiện trạng:** `Permission` entity và `RoleClaim` entity tồn tại, `AuthorizationPolicies.cs` có policy `RequireAdminRole`, nhưng controllers chỉ dùng `[Authorize]` chung chung.

**Phương án (không phức tạp hóa):**
1. Load claims của user (RoleClaims) vào JWT khi login — thêm claims từ `RoleClaim` table.
2. Tạo `IPermissionService` để check permission theo code.
3. Tạo `PermissionRequirement` + `PermissionHandler` cho ASP.NET Core Authorization.
4. Đánh `[Authorize(Policy = "permission:users.read")]` trên từng endpoint.

**JWT claims thêm:**
```csharp
// Trong AuthService.LoginAsync, sau khi xác thực thành công:
var roleClaims = await _userRepository.GetRoleClaimsAsync(user.Id, ct);
claims.AddRange(roleClaims.Select(rc => new Claim(rc.ClaimType, rc.ClaimValue)));
```

---

### Feature 6: Tests

**Unit tests** — không cần DB thật, mock dependencies:
- `AuthServiceTests`: LoginAsync (success, wrong password, user not found, empty password hash)
- `UserServiceTests`: CreateUserAsync, GetAllUsersAsync
- `LoginRequestValidatorTests`: valid, empty username, empty password
- `CreateUserCommandValidatorTests`: valid, invalid email, username too long

**Integration tests** — dùng `WebApplicationFactory<Program>` + in-memory DB hoặc Oracle test instance:
- `POST /api/auth/login` — 200, 400, 401
- `POST /api/auth/refresh-token` — 200, 400 (invalid), 401 (expired)
- `POST /api/auth/logout` — 200, 401 (no token)
- `GET /api/auth/me` — 200, 401

---

## Testing Strategy

**Framework:** xUnit + FluentAssertions + NSubstitute (mocking)

**Thư mục test:**
```
tests/
├── Flex.Auth.UnitTests/     → Unit tests, không cần DB
└── Flex.Auth.IntegrationTests/  → Integration tests với WebApplicationFactory
```

**Coverage target:** Tối thiểu 80% cho `AuthService`, `UserService`, và tất cả validators.

**Cách chạy:**
```powershell
dotnet test Flex.Auth.sln
dotnet test Flex.Auth.sln --collect:"XPlat Code Coverage"
```

---

## Boundaries

**Always do:**
- Dùng `Result.Success(...)` / `ValidationException(ResponseCode.X)` — không trả shape khác
- Async EF Core APIs cho mọi DB operation
- Pass `CancellationToken` qua mọi async public method
- Map Oracle table/column names UPPERCASE trong EF configuration
- Dùng `BoolToCharConverter` cho CHAR(1) boolean columns
- Không log raw token, password, JWT, refresh token value, secret key
- Không tiết lộ lý do cụ thể (username sai / password sai) — dùng `InvalidCredentials`
- Update `CLAUDE.md` khi thêm pattern mới hoặc authentication flow mới

**Ask first:**
- Thêm Redis hay bất kỳ infrastructure dependency nào mới
- Thay đổi schema DB hiện tại (rename column, drop table)
- Thay đổi JWT claims structure (ảnh hưởng downstream services)
- Thay đổi cấu trúc `Result` response wrapper
- Thêm OAuth2 / SSO / MFA (ngoài scope spec này)

**Never do:**
- Commit secrets, JWT secret key, Oracle wallet credentials, connection strings vào code
- Dùng raw SQL concatenation — chỉ dùng EF LINQ hoặc `FromSqlInterpolated`
- Đặt business logic trong Repository
- Đặt infrastructure code trong `Flex.Domain`
- Skip `[Authorize]` trên endpoint cần bảo vệ mà không có lý do rõ ràng
- Xóa test đang fail mà không fix nguyên nhân

---

## Success Criteria

- [ ] `POST /api/auth/login` trả cả `accessToken` và `refreshToken`
- [ ] `POST /api/auth/refresh-token` với token hợp lệ → trả cặp token mới (rotation)
- [ ] `POST /api/auth/refresh-token` với token hết hạn hoặc revoked → 401
- [ ] `POST /api/auth/logout` với token hợp lệ → 200, JTI bị ghi vào REVOKED_TOKENS
- [ ] Dùng token đã logout để gọi API → 401
- [ ] `GET /api/auth/me` với token hợp lệ → trả thông tin user hiện tại
- [ ] `POST /api/auth/login` với username/password rỗng → 400 với validation error
- [ ] Unit tests pass: `dotnet test` không có failure
- [ ] Integration tests pass cho toàn bộ endpoint mới
- [ ] `dotnet build Flex.Auth.sln` không có warning hay error

---

## Open Questions

1. **Refresh token lifetime:** Dùng 7 ngày hay giá trị khác? Có cần config trong `appsettings.json` không?
2. **Revoked token cleanup:** Dùng `OutboxProcessorBackgroundService` sẵn có để cleanup `REVOKED_TOKENS` cũ, hay tạo background service riêng?
3. **RBAC scope:** Feature 5 (RBAC) có nằm trong sprint hiện tại không, hay defer sang sau khi 1–4 xong?
4. **Integration test DB:** Dùng Oracle test instance thật, hay SQLite in-memory (cần EF provider khác)?
5. **LoginResult response shape:** Thêm `refreshToken` vào `LoginResult` record hiện tại, hay tạo `LoginResultV2`?
