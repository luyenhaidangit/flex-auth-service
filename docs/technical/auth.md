# Tài liệu thiết kế Auth Service

## 1. Tổng quan kiến trúc

Auth Service là service trung tâm chịu trách nhiệm xử lý xác thực, phát hành JWT, refresh token, đăng xuất, blacklist token và cung cấp thông tin context người dùng cho các API khác.

Luồng tổng quan:

```text
┌────────────────────────────────────────────────────────────┐
│ Auth Service                                                │
│                                                            │
│ AuthController       → AuthService → JwtTokenService       │
│ ConnectController    → RefreshTokenRepo (DB)               │
│ EmbedAuthController  → TokenBlacklistService               │
│                     → IdentityProvider (SSO)               │
└────────────────────────────────────────────────────────────┘

JWT issued
    │
    ▼

┌────────────────────────────────────────────────────────────┐
│ Mọi API khác (Resource Servers)                            │
│                                                            │
│ AuthContextHandlerMiddleware                               │
│ ├─ Parse JWT                                                │
│ ├─ Check Blacklist                                          │
│ └─ IContextService.SetContext()                             │
└────────────────────────────────────────────────────────────┘
```

### Thành phần chính

| Thành phần | Vai trò |
|---|---|
| `AuthController` | Xử lý API login, refresh token, logout, verify token |
| `ConnectController` | Xử lý OAuth2 client credentials |
| `EmbedAuthController` | Cấp token cho tích hợp nhúng iframe/widget |
| `AuthService` | Xử lý business logic xác thực |
| `JwtTokenService` | Tạo và xác thực JWT |
| `RefreshTokenRepo` | Lưu và truy vấn refresh token trong DB |
| `TokenBlacklistService` | Quản lý danh sách token bị revoke |
| `IdentityProvider` | Tích hợp SSO nếu có |
| `AuthContextHandlerMiddleware` | Parse JWT, check blacklist, set context cho request |
| `IContextService` | Lưu thông tin user/tenant/db shard trong request hiện tại |

---

## 2. Cấu trúc Solution

```text
YourProject.Auth/
├── YourProject.Auth.Api/                         # ASP.NET Core host
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ConnectController.cs                  # OAuth2 client credentials
│   │   └── EmbedAuthController.cs                # Token cho tích hợp nhúng
│   └── Program.cs / Startup.cs
│
├── YourProject.Auth.Application/                 # Business logic
│   ├── Auth/
│   ├── AuthService.cs
│   └── OAuthTokenService.cs
│
├── YourProject.Auth.Application.Contracts/       # Interfaces + DTOs
│   ├── Auth/
│   ├── IAuthService.cs
│   ├── Dtos/
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   └── TokenRequestModel.cs
│
├── YourProject.Auth.Domain/                      # Repo interfaces + Entities
│   ├── Auth/
│   ├── IRefreshTokenRepo.cs
│   ├── RefreshTokenEntity.cs
│   └── TokenBlacklistEntity.cs
│
├── YourProject.Auth.Domain.Mysql/                # Repo implementations
│   ├── Auth/
│   └── MysqlRefreshTokenRepo.cs
│
YourProject.HostBase/                             # Shared - tất cả API dùng
└── Middlewares/
    └── AuthContextHandlerMiddleware.cs
```

---

## 3. Database Schema

### 3.1. Bảng `refresh_token`

Bảng `refresh_token` dùng để lưu refresh token đã được mã hóa/hash.

Không lưu refresh token raw/plain text vào DB.

```sql
CREATE TABLE refresh_token (
    id INT AUTO_INCREMENT PRIMARY KEY,
    token_hash VARCHAR(512) NOT NULL, -- AES-encrypted hash
    user_id CHAR(36) NULL,
    created_date DATETIME NOT NULL DEFAULT NOW(),
    modified_date DATETIME NULL,
    expired_date DATETIME NOT NULL,
    is_revoked TINYINT(1) NOT NULL DEFAULT 0,
    INDEX idx_token_hash (token_hash)
);
```

### Ý nghĩa các trường

| Cột | Kiểu dữ liệu | Ý nghĩa |
|---|---:|---|
| `id` | `INT AUTO_INCREMENT` | Khóa chính |
| `token_hash` | `VARCHAR(512)` | Token đã được mã hóa/hash |
| `user_id` | `CHAR(36)` | ID người dùng |
| `created_date` | `DATETIME` | Thời điểm tạo token |
| `modified_date` | `DATETIME` | Thời điểm cập nhật token |
| `expired_date` | `DATETIME` | Thời điểm refresh token hết hạn |
| `is_revoked` | `TINYINT(1)` | Đánh dấu token đã bị thu hồi hay chưa |

---

### 3.2. Bảng `token_blacklist`

Bảng `token_blacklist` dùng khi cần force logout hoặc revoke access token trước thời điểm hết hạn.

```sql
CREATE TABLE token_blacklist (
    id INT AUTO_INCREMENT PRIMARY KEY,
    jti VARCHAR(128) NOT NULL, -- JWT ID
    expired_date DATETIME NOT NULL,
    created_date DATETIME NOT NULL DEFAULT NOW(),
    UNIQUE INDEX uq_jti (jti)
);
```

### Ý nghĩa các trường

| Cột | Kiểu dữ liệu | Ý nghĩa |
|---|---:|---|
| `id` | `INT AUTO_INCREMENT` | Khóa chính |
| `jti` | `VARCHAR(128)` | JWT ID, định danh duy nhất của token |
| `expired_date` | `DATETIME` | Thời điểm token hết hạn |
| `created_date` | `DATETIME` | Thời điểm đưa token vào blacklist |

---

## 4. Cấu hình `appsettings.json`

```json
{
  "AuthConfig": {
    "JwtToken": {
      "SecretKey": "your-256-bit-secret-key-minimum-32-chars",
      "Issuer": "your-app.com",
      "ExpireSecond": 3600
    },
    "RefreshToken": {
      "Password": "aes-encrypt-password",
      "Salt": "aes-encrypt-salt",
      "ExpireSecond": 2592000
    }
  }
}
```

### Ý nghĩa cấu hình

| Config | Ý nghĩa |
|---|---|
| `AuthConfig:JwtToken:SecretKey` | Secret key dùng để ký JWT. Tối thiểu 256-bit, không hard-code trong source |
| `AuthConfig:JwtToken:Issuer` | Đơn vị phát hành token |
| `AuthConfig:JwtToken:ExpireSecond` | Thời gian sống của access token, ví dụ 3600 giây = 1 giờ |
| `AuthConfig:RefreshToken:Password` | Password dùng cho cơ chế mã hóa refresh token |
| `AuthConfig:RefreshToken:Salt` | Salt dùng cho mã hóa/hash refresh token |
| `AuthConfig:RefreshToken:ExpireSecond` | Thời gian sống của refresh token, ví dụ 2592000 giây = 30 ngày |

---

## 5. JWT Claims Payload

### 5.1. Class `ContextData`

```csharp
public class ContextData
{
    public Guid? UserId { get; set; } // sub
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public bool IsAdmin { get; set; }

    // Nếu có multi-tenant
    public Guid? TenantId { get; set; }
    public string TenantCode { get; set; }
    public string DbCoreId { get; set; } // DB shard lookup

    public string SessionId { get; set; } // jti - dùng cho blacklist
}
```

### 5.2. Ý nghĩa claim

| Claim | Ý nghĩa |
|---|---|
| `sub` | User ID |
| `UserName` | Tên đăng nhập hoặc email |
| `FullName` | Họ tên người dùng |
| `Email` | Email người dùng |
| `IsAdmin` | Người dùng có phải admin hay không |
| `TenantId` | ID tenant nếu hệ thống multi-tenant |
| `TenantCode` | Mã tenant |
| `DbCoreId` | ID database/shard để resolve connection string |
| `jti` | JWT ID, dùng để blacklist token |
| `exp` | Thời điểm token hết hạn |
| `iss` | Issuer của token |

### 5.3. Ví dụ token payload

```json
{
  "sub": "guid-user",
  "UserName": "user@example.com",
  "FullName": "Nguyễn Văn A",
  "TenantId": "guid-tenant",
  "TenantCode": "COMPANY_001",
  "DbCoreId": "db-shard-1",
  "IsAdmin": "false",
  "jti": "unique-token-id",
  "exp": 1234567890,
  "iss": "your-app.com"
}
```

---

## 6. `IAuthService` Interface

```csharp
public interface IAuthService
{
    // Đăng nhập bằng username/password nội bộ
    Task<LoginResponse> LoginAsync(LoginRequest request, HttpResponse response);

    // Đăng nhập qua SSO bên ngoài nếu có
    Task<LoginResponse> LoginWithSSOAsync(string ssoToken, HttpResponse response, HttpRequest request);

    // Chọn tenant nếu user thuộc nhiều tenant
    Task<LoginResponse> GetTokenForTenantAsync(Guid tenantId, TokenRequestModel request, HttpResponse response);

    // Làm mới access token
    Task<LoginResponse> RefreshTokenAsync(TokenRequestModel request);

    // Đăng xuất
    Task LogoutAsync(HttpResponse response);

    // Xác thực token
    Task<bool> VerifyTokenAsync(string token);

    // Tạo token từ thông tin user dùng nội bộ/integration
    Task<string> GenerateTokenAsync(ContextData contextData, DateTime expires, string jti = null);
}
```

---

## 7. Luồng triển khai chi tiết

## 7.1. Login cơ bản username/password

### API

```http
POST /auth/login
```

### Request Body

```json
{
  "username": "user@example.com",
  "password": "password"
}
```

### Luồng xử lý

```text
AuthService.LoginAsync()
├─ 1. Xác thực credentials → query UserRepo
├─ 2. Kiểm tra account: is_active, is_locked
├─ 3. Nếu multi-tenant: query tenant của user
│  ├─ 1 tenant → tự động set TenantId
│  └─ nhiều tenant → trả về danh sách, client chọn
├─ 4. Build ContextData (user info + tenant info + db shard)
├─ 5. GenerateTokenAsync() → JWT (HMAC SHA-256)
├─ 6. CreateRefreshTokenAsync()
│  ├─ Sinh random token string
│  ├─ Mã hóa AES → token_hash
│  ├─ Lưu DB: refresh_token table
│  └─ Set Cookie HttpOnly: "refresh_token"
└─ 7. Trả về LoginResponse { AccessToken, ExpiresIn, TokenType }
```

### Response trường hợp login thành công

```json
{
  "accessToken": "jwt-token-value",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### Ghi chú xử lý multi-tenant

Nếu user chỉ thuộc một tenant:

```text
Login thành công → JWT có đầy đủ TenantId, TenantCode, DbCoreId
```

Nếu user thuộc nhiều tenant:

```text
Login thành công bước 1 → JWT chưa có tenant đầy đủ
Client gọi tiếp /auth/{tenantId}/token để chọn tenant
```

---

## 7.2. Chọn Tenant nếu multi-tenant

### API

```http
POST /auth/{tenantId}/token
```

### Request Body

```json
{
  "accessToken": "<token bước 1>"
}
```

### Luồng xử lý

```text
AuthService.GetTokenForTenantAsync()
├─ 1. Validate JWT cũ, verify signature, ignore lifetime
├─ 2. Kiểm tra: unixNow > exp + RefreshToken.ExpireSecond → "Session expired"
├─ 3. Query DatabaseRepo → DbCoreId, DbConversationId theo tenantId
├─ 4. Build ContextData đầy đủ user + tenant + DB shards
├─ 5. GenerateTokenAsync() → JWT mới có tenant
└─ 6. Trả về JWT mới
```

### Response

```json
{
  "accessToken": "new-jwt-token-with-tenant",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

---

## 7.3. `GenerateTokenAsync` core

```csharp
protected async Task<string> GenerateTokenAsync(ContextData ctx, DateTime expires, string jti = null)
{
    jti ??= Guid.CreateVersion7().ToString("N");

    var key = new SymmetricSecurityKey(
        Encoding.ASCII.GetBytes(_config.JwtToken.SecretKey));

    var creds = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, ctx.UserId.ToString()),
        new(JwtRegisteredClaimNames.Jti, jti),
        new(TokenKeys.UserName, ctx.UserName ?? ""),
        new(TokenKeys.FullName, ctx.FullName ?? ""),
        new(TokenKeys.TenantId, ctx.TenantId?.ToString() ?? ""),
        new(TokenKeys.TenantCode, ctx.TenantCode ?? ""),
        new(TokenKeys.DbCoreId, ctx.DbCoreId ?? ""),
        new(TokenKeys.IsAdmin, ctx.IsAdmin.ToString()),
    };

    var token = new JwtSecurityToken(
        issuer: _config.JwtToken.Issuer,
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Ghi chú

- `jti` là định danh duy nhất của JWT.
- `jti` được dùng để blacklist token khi logout hoặc force logout.
- `SecretKey` phải đủ mạnh, tối thiểu 256-bit.
- Thuật toán ký token là `HMAC SHA-256`.
- `ClockSkew` nên cấu hình `TimeSpan.Zero` để token hết hạn chính xác.

---

## 7.4. Refresh Token

### API

```http
POST /auth/refresh-token
```

### Request Body

```json
{
  "accessToken": "<expired token>"
}
```

### Cookie

```http
refresh_token=<value>
```

### Luồng xử lý

```text
AuthService.RefreshTokenAsync()
├─ 1. Parse JWT ignore lifetime validation
├─ 2. Lấy exp từ payload
├─ 3. Kiểm tra: unixNow > exp + RefreshToken.ExpireSecond → "Session expired"
├─ 4. Lấy refresh_token từ Cookie
├─ 5. AES hash → query DB → kiểm tra:
│  ├─ Not found → "Invalid refresh token"
│  ├─ is_revoked = true → "Token revoked"
│  └─ expired_date < now → "Token expired"
├─ 6. Build ContextData từ claims token cũ
├─ 7. GenerateTokenAsync() → JWT mới
└─ 8. Trả về { AccessToken mới }
```

### Response

```json
{
  "accessToken": "new-access-token",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### Ghi chú bảo mật

- Refresh token nên lưu trong cookie `HttpOnly`.
- Không truyền refresh token qua body nếu không cần thiết.
- Access token hết hạn vẫn được parse để lấy claims, nhưng phải bỏ qua lifetime validation.
- Refresh token trong DB phải kiểm tra `is_revoked` và `expired_date`.

---

## 7.5. Logout

### API

```http
POST /auth/logout
```

### Header

```http
Authorization: Bearer <token>
```

### Cookie

```http
refresh_token=<value>
```

### Luồng xử lý

```text
AuthService.LogoutAsync()
├─ 1. Parse Bearer token → lấy jti
├─ 2. Tùy chọn: Blacklist jti:
│     TokenBlacklistService.BlacklistAsync(jti, expiredDate)
├─ 3. Xóa permission cache: Remove("UserPermission:{uid}:{tid}")
├─ 4. Lấy refresh_token từ Cookie
├─ 5. AES hash → DB update: is_revoked = true
└─ 6. Xóa Cookie: refresh_token, tenant-id
```

### Response

```json
{
  "success": true,
  "message": "Logout successfully"
}
```

---

## 8. `AuthContextHandlerMiddleware`

Middleware này được dùng trong các Resource Server/API khác để:

- Bỏ qua các endpoint public
- Kiểm tra Bearer token
- Kiểm tra blacklist token
- Set context người dùng vào `IContextService`
- Push thông tin user/tenant vào logging scope

### Code middleware

```csharp
public async Task Invoke(HttpContext context, IContextService contextService)
{
    // 1. Bỏ qua nếu AllowAnonymous hoặc swagger/health
    var endpoint = context.GetEndpoint();

    if (endpoint?.Metadata.Any(m => m is IAllowAnonymousAttribute) == true)
    {
        await _next(context);
        return;
    }

    // 2. Bỏ qua các path hệ thống
    var skipPaths = new[] { "swagger", "favicon", "healthz" };

    if (skipPaths.Any(p => context.Request.Path.Value?.Contains(p) == true))
    {
        await _next(context);
        return;
    }

    // 3. Xử lý API Key internal services
    if (!context.User.Identity.IsAuthenticated
        && context.Request.Headers.ContainsKey("X-Api-Key"))
    {
        await _next(context);
        return;
    }

    // 4. Validate Bearer token
    var auth = context.Request.Headers["Authorization"].ToString();

    if (!auth.StartsWith("Bearer "))
    {
        context.Response.StatusCode = 401;
        return;
    }

    var token = auth[7..];

    if (!await ValidateTokenAsync(context, token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    // 5. Push NLog scope
    var ctx = contextService.GetContext();

    using (NLog.ScopeContext.PushProperties(new Dictionary<string, string>
    {
        ["UserId"] = ctx?.UserId?.ToString() ?? "",
        ["UserName"] = ctx?.UserName ?? "",
        ["TenantId"] = ctx?.TenantId?.ToString() ?? ""
    }))
    {
        await _next(context);
    }
}
```

### Hàm `ValidateTokenAsync`

```csharp
private async Task<bool> ValidateTokenAsync(HttpContext ctx, string token)
{
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    // Kiểm tra blacklist theo jti
    if (jwt.Payload.TryGetValue(JwtRegisteredClaimNames.Jti, out var jtiObj)
        && jtiObj is string jti)
    {
        var blacklistSvc = ctx.RequestServices
            .GetRequiredService<ITokenBlacklistService>();

        if (await blacklistSvc.IsBlacklistedAsync(jti))
            return false;
    }

    return true;
}
```

### Lưu ý quan trọng

`AuthContextHandlerMiddleware` không thay thế hoàn toàn `UseAuthentication`.

Pipeline vẫn cần:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseSetAuthContextHandler();
```

Trong đó:

- `UseAuthentication()` kiểm tra JWT signature, issuer, lifetime.
- `UseAuthorization()` kiểm tra quyền truy cập endpoint.
- `UseSetAuthContextHandler()` kiểm tra blacklist và set context.

---

## 9. `Startup.cs` Wiring up

### 9.1. ConfigureServices

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(config["AuthConfig:JwtToken:SecretKey"])),

            ValidateIssuer = true,
            ValidIssuer = config["AuthConfig:JwtToken:Issuer"],

            ValidateAudience = false,
            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IRefreshTokenRepo, MysqlRefreshTokenRepo>();
services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();
services.AddScoped<IContextService, ContextService>();
```

### 9.2. Configure pipeline

```csharp
app.UseCors();
app.UseRouting();

app.UseAuthentication(); // ASP.NET Core validate JWT signature/expiry
app.UseAuthorization();

app.UseSetAuthContextHandler(); // Custom middleware: blacklist + SetContext

app.UseEndpoints(...);
```

### Thứ tự pipeline khuyến nghị

```text
UseCors()
UseRouting()
UseAuthentication()
UseAuthorization()
UseSetAuthContextHandler()
UseEndpoints()
```

---

## 10. API Endpoints tổng hợp

| Method | Route | Auth | Mô tả |
|---|---|---|---|
| POST | `/auth/login` | Anonymous | Login username/password |
| POST | `/auth/{tenantId}/token` | Bearer no tenant | Chọn tenant → JWT đầy đủ |
| POST | `/auth/refresh-token` | Cookie | Làm mới access token |
| POST | `/auth/logout` | Bearer | Đăng xuất, revoke token |
| POST | `/auth/token/verify` | Anonymous | Xác thực token hợp lệ không |
| POST | `/connect/token` | Anonymous | OAuth2 client credentials |
| POST | `/embedauth/token` | Anonymous | Token nhúng iframe/widget |

---

## 11. Các điểm quan trọng cần lưu ý khi triển khai

## 11.1. Bảo mật

- Refresh token phải mã hóa AES trước khi lưu DB.
- Chỉ lưu `token_hash`, không lưu plain refresh token.
- Cookie refresh token phải cấu hình:
  - `HttpOnly=true`
  - `Secure=true`
  - `SameSite=Strict`
- JWT secret key tối thiểu 256-bit.
- JWT secret key phải lưu trong secret manager, không hard-code.
- Blacklist nên dùng Redis.
- TTL của blacklist bằng thời gian hết hạn còn lại của token.
- Không nên lưu blacklist access token lâu hơn thời gian sống thực tế của token.

---

## 11.2. Multi-tenant

Luồng multi-tenant gồm 2 bước.

### Bước 1: Login chưa có tenant

```text
User login username/password
→ JWT chứa user info
→ TenantId rỗng hoặc chưa đầy đủ
→ Trả danh sách tenant cho client nếu user có nhiều tenant
```

### Bước 2: Chọn tenant

```text
Client chọn tenant
→ Gọi /auth/{tenantId}/token
→ JWT mới có đầy đủ tenant + DbCoreId
→ Các API khác dùng DbCoreId để resolve đúng connection string
```

### Lưu ý

`DbCoreId` trong JWT dùng để `MysqlBaseTenantRepo` resolve đúng connection string.

Không nên để client tự truyền connection string.

---

## 11.3. Performance

- Permission cache theo key:

```text
UserPermission:{userId}:{tenantId}
```

- Xóa cache ngay khi:
  - User logout
  - Admin thay đổi quyền user
  - User bị khóa
  - Tenant permission thay đổi

- Token blacklist nên dùng Redis SET với TTL để tránh query DB mỗi request.
- Không query database ở mọi request chỉ để kiểm tra token nếu có thể dùng Redis.
- Access token nên có thời gian sống ngắn, ví dụ 15 phút đến 1 giờ.

---

## 11.4. Session expiry logic

```text
AccessToken.exp = thời điểm token hết hạn ngắn, ví dụ 1 giờ
RefreshToken.expired_date = thời điểm phiên làm việc hết hạn dài, ví dụ 30 ngày
Refresh được phép khi: unixNow <= exp + RefreshToken.ExpireSecond
```

### Ví dụ

```text
Access token hết hạn lúc: 10:00
RefreshToken.ExpireSecond = 2592000 giây = 30 ngày

Người dùng được refresh token nếu thời điểm hiện tại <= 10:00 + 30 ngày.
```

---

## 12. Checklist triển khai Auth Service

### 12.1. Database

- [ ] Tạo bảng `refresh_token`
- [ ] Tạo index `idx_token_hash`
- [ ] Tạo bảng `token_blacklist` nếu cần blacklist bằng DB
- [ ] Tạo unique index `uq_jti`
- [ ] Kiểm tra timezone của DB và application

---

### 12.2. Configuration

- [ ] Cấu hình `AuthConfig:JwtToken:SecretKey`
- [ ] Cấu hình `AuthConfig:JwtToken:Issuer`
- [ ] Cấu hình `AuthConfig:JwtToken:ExpireSecond`
- [ ] Cấu hình `AuthConfig:RefreshToken:Password`
- [ ] Cấu hình `AuthConfig:RefreshToken:Salt`
- [ ] Cấu hình `AuthConfig:RefreshToken:ExpireSecond`
- [ ] Đưa secret ra Secret Manager hoặc biến môi trường

---

### 12.3. Backend implementation

- [ ] Implement `IAuthService`
- [ ] Implement `AuthService.LoginAsync`
- [ ] Implement `AuthService.RefreshTokenAsync`
- [ ] Implement `AuthService.LogoutAsync`
- [ ] Implement `AuthService.VerifyTokenAsync`
- [ ] Implement `GenerateTokenAsync`
- [ ] Implement `IRefreshTokenRepo`
- [ ] Implement `MysqlRefreshTokenRepo`
- [ ] Implement `ITokenBlacklistService`
- [ ] Implement `RedisTokenBlacklistService`
- [ ] Implement `IContextService`
- [ ] Implement `ContextService`
- [ ] Implement `AuthContextHandlerMiddleware`

---

### 12.4. API

- [ ] Tạo `AuthController`
- [ ] Tạo API `POST /auth/login`
- [ ] Tạo API `POST /auth/{tenantId}/token`
- [ ] Tạo API `POST /auth/refresh-token`
- [ ] Tạo API `POST /auth/logout`
- [ ] Tạo API `POST /auth/token/verify`
- [ ] Tạo `ConnectController`
- [ ] Tạo API `POST /connect/token`
- [ ] Tạo `EmbedAuthController`
- [ ] Tạo API `POST /embedauth/token`

---

### 12.5. Middleware

- [ ] Đăng ký JWT Authentication
- [ ] Cấu hình `TokenValidationParameters`
- [ ] Cấu hình `ClockSkew = TimeSpan.Zero`
- [ ] Gọi `app.UseAuthentication()`
- [ ] Gọi `app.UseAuthorization()`
- [ ] Gọi `app.UseSetAuthContextHandler()`
- [ ] Kiểm tra đúng thứ tự middleware
- [ ] Bỏ qua swagger, favicon, healthz
- [ ] Bỏ qua endpoint có `AllowAnonymous`
- [ ] Check blacklist theo `jti`
- [ ] Set context user/tenant/db shard
- [ ] Push thông tin context vào NLog scope

---

### 12.6. Security testing

- [ ] Login thành công với username/password đúng
- [ ] Login thất bại với password sai
- [ ] Login thất bại khi user bị lock
- [ ] Login thất bại khi user inactive
- [ ] Refresh token thành công khi access token hết hạn
- [ ] Refresh token thất bại khi refresh token expired
- [ ] Refresh token thất bại khi refresh token revoked
- [ ] Logout xóa cookie refresh token
- [ ] Logout revoke refresh token
- [ ] Logout blacklist access token nếu cần
- [ ] Token bị blacklist không gọi được API
- [ ] Token hết hạn không gọi được API
- [ ] Token sai issuer không gọi được API
- [ ] Token sai signature không gọi được API
- [ ] Cookie refresh token có `HttpOnly`
- [ ] Cookie refresh token có `Secure`
- [ ] Cookie refresh token có `SameSite=Strict`

---

## 13. Gợi ý cấu trúc DTO

### 13.1. `LoginRequest`

```csharp
public class LoginRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
```

### 13.2. `TokenRequestModel`

```csharp
public class TokenRequestModel
{
    public string AccessToken { get; set; }
}
```

### 13.3. `LoginResponse`

```csharp
public class LoginResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";

    // Dùng khi user có nhiều tenant
    public bool RequireTenantSelection { get; set; }
    public List<TenantDto> Tenants { get; set; }
}
```

### 13.4. `TenantDto`

```csharp
public class TenantDto
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; }
    public string TenantName { get; set; }
}
```

---

## 14. Gợi ý cấu trúc Entity

### 14.1. `RefreshTokenEntity`

```csharp
public class RefreshTokenEntity
{
    public int Id { get; set; }
    public string TokenHash { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public bool IsRevoked { get; set; }
}
```

### 14.2. `TokenBlacklistEntity`

```csharp
public class TokenBlacklistEntity
{
    public int Id { get; set; }
    public string Jti { get; set; }
    public DateTime ExpiredDate { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

---

## 15. Gợi ý interface repository

### 15.1. `IRefreshTokenRepo`

```csharp
public interface IRefreshTokenRepo
{
    Task CreateAsync(RefreshTokenEntity entity);
    Task<RefreshTokenEntity> GetByTokenHashAsync(string tokenHash);
    Task RevokeAsync(string tokenHash);
}
```

### 15.2. `ITokenBlacklistService`

```csharp
public interface ITokenBlacklistService
{
    Task BlacklistAsync(string jti, DateTime expiredDate);
    Task<bool> IsBlacklistedAsync(string jti);
}
```

---

## 16. Tổng kết

Thiết kế Auth Service này phù hợp cho hệ thống:

- ASP.NET Core API
- Microservice
- Multi-tenant
- Có nhu cầu refresh token
- Có nhu cầu force logout
- Có nhiều Resource Server cần dùng chung authentication context
- Có phân quyền theo user/tenant
- Có thể mở rộng sang OAuth2 client credentials và SSO

Nguyên tắc quan trọng nhất:

```text
Auth Service phát hành token.
Resource Server chỉ validate token, check blacklist và set context.
Refresh token phải được bảo vệ bằng HttpOnly Cookie.
Không lưu plain refresh token.
Không hard-code secret key.
Multi-tenant phải resolve DB qua DbCoreId hoặc mapping server-side, không để client truyền connection string.
```