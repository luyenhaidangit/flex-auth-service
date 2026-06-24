# Spec: flex-auth-service — Realtime Force-Logout (SignalR)

> Tài liệu này đặc tả tính năng **realtime force-logout** cho `flex-auth-service`.
> Đây là feature riêng, **không thay thế** `SPEC.md` (refresh token / logout / RBAC / tests).
> Hai spec dùng chung hạ tầng revoke token nên cần đọc kèm nhau khi implement.

## Objective

Cho phép **admin** buộc một user đang đăng nhập phải đăng xuất **ngay lập tức**, không phải chờ access token hết hạn. Khi admin thực hiện force-logout:

1. Mọi access token đã phát cho user đó bị vô hiệu hóa (các request kế tiếp trả `401`).
2. Các tab/phiên của user đang mở trong **admin dashboard** nhận tín hiệu realtime qua SignalR và tự logout (xóa token, redirect về trang login) mà không cần reload thủ công.

**Target users:**
- **Admin** (qua flex-microfrontend dashboard): người kích hoạt force-logout.
- **User bị tác động** đang mở dashboard: nhận push realtime và bị logout.
- Gián tiếp: đội bảo mật/vận hành cần khả năng cắt phiên tức thời (tài khoản bị lộ, nhân sự nghỉ việc, phát hiện bất thường).

**Scope đã chốt với người dùng:**
| Quyết định | Lựa chọn |
|---|---|
| Use case | Force-logout / revoke session realtime |
| Transport | **SignalR** (built-in ASP.NET Core, không dependency ngoài) |
| Scale-out | **Single-instance trước mắt**; backplane để giai đoạn sau (qua abstraction) |
| Client tiêu thụ | **Admin dashboard** (flex-microfrontend) |

**Success conditions (tổng quan):**
- Admin gọi force-logout cho user X → các tab dashboard của X nhận event `ForceLogout` trong < 2s và tự logout.
- Sau force-logout, token cũ của X dùng gọi bất kỳ API nào → `401`.
- Force-logout là idempotent: gọi nhiều lần không gây lỗi.
- Single-instance: không thêm Redis, không thêm message broker mới cho realtime.

---

## Tech Stack

| Thành phần | Version / Ghi chú |
|---|---|
| Runtime | .NET 9.0 (`net9.0`, nullable enabled, implicit usings) |
| Framework | ASP.NET Core Web API + **SignalR** (`Microsoft.AspNetCore.SignalR`, có sẵn trong shared framework) |
| Realtime transport | SignalR (WebSocket ưu tiên, fallback SSE/long-polling tự động) |
| Database | Oracle via EF Core 9 (Oracle.EntityFrameworkCore), UPPERCASE table/column |
| Auth | JWT Bearer (HMAC-SHA256), `JwtSettings`, `TokenService` có sẵn |
| Messaging | RabbitMQ + Outbox (chỉ dùng cho **audit event**, không phải backplane realtime) |
| Cache | `IMemoryCache` (built-in) cho revocation watermark — single-instance |
| Logging | Serilog + correlation ID có sẵn |
| Testing | xUnit + FluentAssertions + NSubstitute |

**Không thêm:** Redis, SignalR backplane (Redis/Azure SignalR), dependency NuGet mới ngoài `Microsoft.AspNetCore.SignalR` (đã nằm trong framework reference của `Microsoft.NET.Sdk.Web`, nên thực tế **không cần thêm package**).

**Lý do single-instance hợp lệ:** `IHubContext` chỉ broadcast tới connection trên cùng process. Với 1 instance, `Clients.User(userName)` đủ. Khi scale-out, chỉ cần thay implementation của `ISessionNotifier` bằng bản có backplane — hợp đồng (interface) giữ nguyên.

---

## Commands

```powershell
# Restore
dotnet restore Flex.Auth.sln

# Build toàn bộ solution
dotnet build Flex.Auth.sln

# Chạy local (development)
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Flex.Auth/Flex.Auth.csproj

# Chạy tests
dotnet test Flex.Auth.sln

# Tests + coverage
dotnet test Flex.Auth.sln --collect:"XPlat Code Coverage"

# Lint (nếu dotnet-format đã cài)
dotnet format Flex.Auth.sln --verify-no-changes
```

URLs local:
```
HTTP  : http://localhost:5050
HTTPS : https://localhost:7040
SignalR hub: https://localhost:7040/hubs/session
Swagger : https://localhost:7040/swagger/ui (Development only)
```

Smoke test realtime (sau khi implement) — dùng 2 phiên đăng nhập, gọi force-logout, quan sát phiên còn lại bị đẩy `ForceLogout`.

---

## Project Structure

Chỉ liệt kê file **mới/đổi** cho feature này (giữ nguyên phần còn lại của repo):

```
flex-auth-service/
├── src/
│   ├── Flex.Auth/
│   │   ├── Hubs/                                    # MỚI
│   │   │   └── SessionHub.cs                        # SignalR hub [Authorize], client kết nối tại /hubs/session
│   │   ├── Controllers/
│   │   │   └── SessionsController.cs                # MỚI: POST /api/sessions/force-logout (admin)
│   │   ├── Services/
│   │   │   ├── SessionService.cs                    # MỚI: ForceLogoutAsync(userName, reason, ct)
│   │   │   └── Interfaces/
│   │   │       └── ISessionService.cs               # MỚI
│   │   ├── Realtime/                                # MỚI: lớp đẩy realtime (abstraction)
│   │   │   ├── ISessionNotifier.cs                  # MỚI: NotifyForceLogoutAsync(...)
│   │   │   ├── SignalRSessionNotifier.cs            # MỚI: impl bọc IHubContext<SessionHub>
│   │   │   ├── ForceLogoutNotification.cs           # MỚI: payload gửi client
│   │   │   └── SessionUserIdProvider.cs             # MỚI: IUserIdProvider -> map sub(username)
│   │   ├── Models/Sessions/                         # MỚI
│   │   │   ├── ForceLogoutRequest.cs                # MỚI: { UserName, Reason? }
│   │   │   └── ForceLogoutResult.cs                 # MỚI: { UserName, RevokedAtUtc, ConnectionsNotified? }
│   │   ├── Validators/
│   │   │   └── ForceLogoutRequestValidator.cs       # MỚI (FluentValidation, nếu đã bật theo SPEC.md)
│   │   ├── Repositories/
│   │   │   ├── TokenRevocationRepository.cs         # MỚI: upsert/get watermark
│   │   │   └── Interfaces/
│   │   │       └── ITokenRevocationRepository.cs    # MỚI
│   │   └── Extensions/
│   │       └── ServiceExtensions.cs                 # ĐỔI: AddSignalR, đăng ký hub/notifier/service/repo, sửa CORS
│   │
│   ├── Flex.Domain/
│   │   ├── Entities/
│   │   │   └── UserTokenRevocation.cs               # MỚI: watermark revoke per-user
│   │   └── Events/Users/
│   │       └── UserSessionForceLoggedOutEvent.cs    # MỚI: domain event (audit qua Outbox)
│   │
│   └── Flex.Infrastructures/
│       ├── Authentication/
│       │   ├── AuthenticationExtensions.cs          # ĐỔI: OnMessageReceived đọc access_token query cho hub;
│       │   │                                        #       OnTokenValidated check revocation watermark
│       │   └── ITokenRevocationChecker.cs           # MỚI: trừu tượng check (gọi từ JwtBearerEvents)
│       └── Persistence/
│           ├── IdentityDbContext.cs                 # ĐỔI: DbSet<UserTokenRevocation>
│           └── Configurations/
│               └── UserTokenRevocationConfiguration.cs  # MỚI: map Oracle UPPERCASE
│
└── tests/
    ├── Flex.Auth.UnitTests/
    │   ├── Services/SessionServiceTests.cs          # MỚI
    │   ├── Realtime/SignalRSessionNotifierTests.cs  # MỚI
    │   └── Validators/ForceLogoutRequestValidatorTests.cs  # MỚI
    └── Flex.Auth.IntegrationTests/
        └── Controllers/SessionsControllerTests.cs   # MỚI
```

> `SessionHub` được map trong pipeline `UseInfrastructure()` / `Program.cs`:
> `app.MapHub<SessionHub>("/hubs/session");`

---

## Code Style

Theo đúng convention repo hiện tại (controller mỏng → service → repository; `Result` wrapper; `ValidationException(ResponseCode.X)`; Oracle UPPERCASE; alias `ClaimTypesApp = Flex.Infrastructures.Authentication.ClaimTypes`).

```csharp
// ── Hub: chỉ là điểm kết nối, KHÔNG chứa business logic ──────────────
[Authorize]
public sealed class SessionHub : Hub
{
    // Không cần method server-callable cho force-logout (push 1 chiều từ server).
    // Connection được nhóm theo user thông qua IUserIdProvider (Context.UserIdentifier).
}

// ── IUserIdProvider: map connection -> username (claim sub) ──────────
public sealed class SessionUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(ClaimTypesApp.Sub)?.Value;
}

// ── Payload gửi xuống client ─────────────────────────────────────────
public sealed record ForceLogoutNotification(string Reason, DateTime RevokedAtUtc);

// ── Notifier abstraction: cho phép thay backplane sau này ────────────
public interface ISessionNotifier
{
    Task NotifyForceLogoutAsync(string userName, string reason, CancellationToken ct = default);
}

public sealed class SignalRSessionNotifier : ISessionNotifier
{
    private readonly IHubContext<SessionHub> _hub;
    private readonly ILogger<SignalRSessionNotifier> _logger;

    public SignalRSessionNotifier(IHubContext<SessionHub> hub, ILogger<SignalRSessionNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyForceLogoutAsync(string userName, string reason, CancellationToken ct = default)
    {
        var payload = new ForceLogoutNotification(reason, DateTime.UtcNow);
        await _hub.Clients.User(userName).SendAsync("ForceLogout", payload, ct);
        // KHÔNG log token; chỉ log username + lý do (an toàn).
        _logger.LogInformation("Force-logout pushed to user {UserName}", userName);
    }
}

// ── Service: business logic thuần ────────────────────────────────────
public sealed class SessionService : ISessionService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IUserRepository _userRepository;
    private readonly ITokenRevocationRepository _revocationRepository;
    private readonly ISessionNotifier _notifier;
    private readonly IOutboxWriter _outboxWriter;

    public SessionService(
        IdentityDbContext dbContext,
        IUserRepository userRepository,
        ITokenRevocationRepository revocationRepository,
        ISessionNotifier notifier,
        IOutboxWriter outboxWriter)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _revocationRepository = revocationRepository;
        _notifier = notifier;
        _outboxWriter = outboxWriter;
    }

    public async Task<ForceLogoutResult> ForceLogoutAsync(
        ForceLogoutRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUserNameAsync(request.UserName, ct)
            ?? throw new ValidationException(ResponseCode.UserNotFound);

        var revokedAtUtc = DateTime.UtcNow;

        // 1) Đặt watermark: mọi token có iat < revokedAtUtc sẽ bị từ chối.
        await _revocationRepository.SetRevokeWatermarkAsync(user.Id, revokedAtUtc, ct);

        // 2) Ghi audit event qua Outbox (cùng transaction).
        await _outboxWriter.AddAsync(
            new UserSessionForceLoggedOutEvent(user.Id, user.UserName!, request.Reason, revokedAtUtc), ct);

        await _dbContext.SaveChangesAsync(ct);

        // 3) Đẩy realtime sau khi commit thành công.
        await _notifier.NotifyForceLogoutAsync(user.UserName!, request.Reason ?? "Session revoked", ct);

        return new ForceLogoutResult(user.UserName!, revokedAtUtc);
    }
}

// ── Controller: mỏng, chỉ HTTP concern ───────────────────────────────
[Route("api/[controller]")]
[ApiController]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

    [HttpPost("force-logout")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest request, CancellationToken ct)
    {
        var result = await _sessionService.ForceLogoutAsync(request, ct);
        return Ok(Result.Success(result));
    }
}

// ── Entity: watermark revoke per-user ────────────────────────────────
public class UserTokenRevocation : EntityBase<long>
{
    public long UserId { get; set; }
    public DateTime RevokeBeforeUtc { get; set; }
}

// ── EF config: Oracle UPPERCASE, index theo UserId ───────────────────
public class UserTokenRevocationConfiguration : IEntityTypeConfiguration<UserTokenRevocation>
{
    public void Configure(EntityTypeBuilder<UserTokenRevocation> builder)
    {
        builder.ToTable("USER_TOKEN_REVOCATIONS");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasColumnName("USER_ID").IsRequired();
        builder.Property(x => x.RevokeBeforeUtc).HasColumnName("REVOKE_BEFORE_UTC").IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
```

**Naming:**
- Hub: `SessionHub` (suffix `Hub`).
- Notifier: `ISessionNotifier` / `SignalRSessionNotifier`.
- Service: `ISessionService` / `SessionService`.
- Repository: `ITokenRevocationRepository` / `TokenRevocationRepository`.
- Entity: `UserTokenRevocation` (không suffix `Entity`).
- Domain event: past tense — `UserSessionForceLoggedOutEvent`.
- Client event name (SignalR): `"ForceLogout"` (PascalCase, ổn định — coi như API contract).

**Không làm:**
- Không đặt business logic trong `SessionHub` (hub chỉ là transport).
- Không log token/jwt/refresh token; chỉ log `userName` + lý do.
- Không phát realtime *trước khi* `SaveChangesAsync` commit (tránh đẩy logout cho lần revoke chưa bền vững).

---

## Realtime & Revocation Design

### Cơ chế vô hiệu hóa token (revocation watermark)

`jti` hiện random mỗi lần login và service không lưu danh sách token đã phát → không thể liệt kê từng token. Vì vậy dùng **watermark per-user**:

```
USER_TOKEN_REVOCATIONS { Id, UserId (unique), RevokeBeforeUtc }
```

- Force-logout user X → upsert `RevokeBeforeUtc = now`.
- Mọi access token có `iat` (issued-at) **trước** `RevokeBeforeUtc` đều bị từ chối → cắt **tất cả** phiên của X trong một thao tác.
- Token phát **sau** force-logout (đăng nhập lại) có `iat` mới hơn → hợp lệ.

> Ràng buộc: JWT phải có claim `iat`. `JwtSecurityToken` của .NET tự thêm `iat` mặc định → đã thỏa. Bước implement cần xác nhận `iat` không bị loại bỏ và đọc được trong `OnTokenValidated`.

### Điểm cắm kiểm tra (JWT pipeline)

Trong `AuthenticationExtensions`, thêm vào `JwtBearerEvents.OnTokenValidated`:

```
OnTokenValidated:
  sub  = principal.sub (username)
  iat  = token issued-at (UTC)
  watermark = ITokenRevocationChecker.GetRevokeBeforeUtc(sub)   // cache -> DB
  if watermark != null && iat < watermark:
      ctx.Fail("Session has been revoked.")   // -> 401
```

- `ITokenRevocationChecker` đọc qua `IMemoryCache` (TTL ngắn, ví dụ 30s) để tránh query Oracle mỗi request; force-logout sẽ **invalidate cache** key của user.
- Single-instance: cache đủ tin cậy. Khi scale-out, watermark vẫn nằm ở DB (nguồn chân lý), chỉ TTL cache là độ trễ tối đa.

### Luồng đầu–cuối

```
[Admin dashboard]
   POST /api/sessions/force-logout { userName, reason }   (JWT admin)
        │
        ▼
[SessionsController] --(Authorize: RequireAdminRole)--> [SessionService.ForceLogoutAsync]
        │  1) set watermark (USER_TOKEN_REVOCATIONS)
        │  2) outbox: UserSessionForceLoggedOutEvent
        │  3) SaveChangesAsync (commit)
        │  4) invalidate revocation cache cho user
        │  5) ISessionNotifier.NotifyForceLogoutAsync(userName, reason)
        ▼
[SignalRSessionNotifier] -> IHubContext.Clients.User(userName).SendAsync("ForceLogout", payload)
        │
        ▼
[Admin dashboard hub client]  hub.on("ForceLogout") => clear token + redirect /login

# Song song, mọi request kế tiếp của user bị revoke:
[Any API] -> JwtBearer OnTokenValidated -> iat < watermark? -> 401
```

### Kết nối SignalR & xác thực

- Client mở hub tại `/hubs/session` với JWT.
- WebSocket không gửi header `Authorization` được → SignalR truyền token qua query string `?access_token=...`. Cần mở rộng `OnMessageReceived` để đọc `access_token` khi path bắt đầu bằng `/hubs/session`.
- **CORS:** hiện `AllowAnyOrigin()` — không dùng được với `AllowCredentials()` mà SignalR yêu cầu. Phải đổi sang danh sách origin cụ thể (origin của dashboard) + `AllowCredentials()`. Đây là thay đổi thuộc nhóm **Ask first** (xem Boundaries).

---

## Testing Strategy

**Framework:** xUnit + FluentAssertions + NSubstitute (nhất quán với `SPEC.md`).

**Thư mục:** `tests/Flex.Auth.UnitTests`, `tests/Flex.Auth.IntegrationTests`.

**Unit tests (không cần DB/SignalR thật, mock dependency):**
- `SessionServiceTests`:
  - Force-logout user tồn tại → gọi `SetRevokeWatermarkAsync`, ghi outbox event, gọi `NotifyForceLogoutAsync`, trả `ForceLogoutResult`.
  - User không tồn tại → `ValidationException(ResponseCode.UserNotFound)`, **không** đẩy realtime.
  - Notifier được gọi **sau** `SaveChangesAsync` (verify thứ tự).
  - Idempotent: gọi 2 lần không ném lỗi.
- `SignalRSessionNotifierTests`: verify `Clients.User(userName).SendAsync("ForceLogout", ...)` được gọi với payload đúng (mock `IHubContext`).
- `ForceLogoutRequestValidatorTests`: `UserName` rỗng → invalid; hợp lệ → valid.
- Revocation check: token `iat < watermark` → fail; `iat >= watermark` → pass; không có watermark → pass.

**Integration tests (`WebApplicationFactory<Program>`):**
- `POST /api/sessions/force-logout` không token → `401`.
- Có token nhưng không phải admin → `403`.
- Admin + userName hợp lệ → `200`, bản ghi watermark được tạo.
- Sau force-logout, dùng token cũ của user gọi endpoint `[Authorize]` bất kỳ → `401`.
- (Tùy chọn, nếu khả thi) test realtime: kết nối `HubConnection` test, gọi force-logout, assert nhận được `ForceLogout`.

**Coverage target:** ≥ 80% cho `SessionService`, `SignalRSessionNotifier`, validator, và nhánh revocation-check.

---

## Boundaries

**Always do:**
- Controller mỏng; business logic trong `SessionService`; hub không chứa logic.
- Đẩy realtime **sau** khi commit DB (`SaveChangesAsync`).
- Map Oracle table/column UPPERCASE trong EF configuration; dùng `BoolToCharConverter` cho cột CHAR(1) boolean nếu phát sinh.
- Pass `CancellationToken` qua mọi async public method.
- Dùng `Result.Success(...)` / `ValidationException(ResponseCode.X)` cho response.
- Phát `UserSessionForceLoggedOutEvent` qua Outbox để có audit tin cậy.
- Invalidate cache revocation của user ngay khi force-logout.
- Chỉ log `userName` + lý do; không log token/jwt/secret.

**Ask first:**
- **Đổi CORS** từ `AllowAnyOrigin` sang origin cụ thể + `AllowCredentials` (bắt buộc cho SignalR, nhưng ảnh hưởng mọi client hiện tại).
- **Thay đổi JWT claims / login flow** để nạp **role** vào token (cần thiết để `RequireAdminRole` hoạt động — xem Open Questions #1). Ảnh hưởng downstream services.
- Thêm bất kỳ NuGet/infra mới (Redis, SignalR backplane, broker mới).
- Thay đổi schema bảng hiện có (tạo bảng mới `USER_TOKEN_REVOCATIONS` là additive, không thuộc nhóm này).
- Thay đổi cấu trúc `Result` wrapper hoặc tên event `"ForceLogout"` (là contract với frontend).

**Never do:**
- Commit secrets (JWT key, Oracle wallet, connection string).
- Log raw token, jwt, refresh token, password, secret.
- Đặt business logic trong `SessionHub` hoặc trong Repository.
- Đặt infrastructure code trong `Flex.Domain`.
- Đẩy `ForceLogout` realtime nhưng **không** ghi watermark (sẽ tạo lỗ hổng: token cũ vẫn dùng được sau khi client bỏ qua sự kiện).
- Mở hub mà bỏ `[Authorize]`.
- Dùng raw SQL nối chuỗi; chỉ EF LINQ hoặc `FromSqlInterpolated`.

---

## Success Criteria

- [ ] `POST /api/sessions/force-logout` (admin) với `userName` hợp lệ → `200`, tạo/cập nhật bản ghi `USER_TOKEN_REVOCATIONS` với `RevokeBeforeUtc ≈ now`.
- [ ] Sau force-logout, token cũ của user gọi endpoint `[Authorize]` bất kỳ → `401`.
- [ ] Token mới (đăng nhập lại sau force-logout) → hoạt động bình thường.
- [ ] Tab dashboard của user bị force-logout nhận event SignalR `ForceLogout` (< 2s) và tự logout.
- [ ] Gọi force-logout không token → `401`; token không phải admin → `403`.
- [ ] Force-logout idempotent: gọi lặp không lỗi.
- [ ] Client kết nối `/hubs/session` xác thực được bằng JWT qua `access_token` query string.
- [ ] Không thêm Redis/backplane; chạy đúng trên 1 instance.
- [ ] `dotnet build Flex.Auth.sln` không warning/error.
- [ ] Unit + integration tests pass; coverage ≥ 80% cho các thành phần mới.

---

## Open Questions

1. **Role trong JWT (BLOCKER cho authz admin):** `LoginAsync` hiện **không** nạp role claim, nên `[Authorize(Policy = RequireAdminRole)]` sẽ luôn `403`. Cần:
   (a) nạp role vào JWT khi login (đụng login flow — thuộc Ask first), hoặc
   (b) tạm bảo vệ bằng `RequireAuthenticatedUser` + check thủ công, hoặc
   (c) phối hợp với Feature 5 (RBAC) trong `SPEC.md`.
   → Bạn muốn hướng nào?

2. **Phạm vi "force-logout":** Chỉ admin force-logout **user khác** (đã chốt theo client = admin dashboard), hay cần thêm self-service "logout tất cả thiết bị của tôi"?

3. **Trùng lặp với `SPEC.md` (logout + blacklist):** `SPEC.md` dự kiến `RevokedToken { Jti }` cho logout đơn lẻ. Spec này dùng **watermark per-user** cho force-logout toàn bộ. Hai cơ chế bổ trợ nhau. Có cần hợp nhất thành một `ITokenRevocationChecker` chung (kiểm tra cả jti-blacklist lẫn watermark) không, hay làm watermark trước, blacklist sau?

4. **CORS origins:** Origin chính xác của admin dashboard (flex-microfrontend) là gì (dev/staging/prod) để cấu hình allow-list + `AllowCredentials`?

5. **TTL cache revocation:** 30s có chấp nhận được không? Đây là độ trễ tối đa giữa lúc set watermark và lúc token cũ bị chặn ở các instance khác (hiện single-instance nên gần như tức thời do invalidate trực tiếp).

6. **Audit event consumers:** `UserSessionForceLoggedOutEvent` phát qua Outbox/RabbitMQ — có downstream service nào cần subscribe ngay (vd: ghi audit log tập trung), hay chỉ để dành?

7. **Endpoint shape:** `POST /api/sessions/force-logout { userName }` ổn chứ, hay bạn muốn `POST /api/sessions/{userName}/force-logout` (path param)?

---

## Next Phases (sau khi spec được duyệt)

Theo gated workflow của spec-driven-development:

- **PLAN** — thứ tự triển khai đề xuất: (1) entity + EF config + DbSet + repository → (2) `ITokenRevocationChecker` + cắm `OnTokenValidated` → (3) `SessionService` + controller + validator → (4) SignalR hub + `IUserIdProvider` + notifier + DI/CORS/`access_token` query → (5) tests → (6) tích hợp client dashboard.
- **TASKS** — bẻ nhỏ từng phase thành task ≤ ~5 file, mỗi task có acceptance + verify.
- **IMPLEMENT** — thực thi tuần tự theo incremental-implementation + TDD.

> **Chưa code gì cho tới khi bạn duyệt spec này và trả lời các Open Questions (đặc biệt #1 — blocker authz).**
