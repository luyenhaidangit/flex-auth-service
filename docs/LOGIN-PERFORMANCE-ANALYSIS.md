# 🔍 Phân Tích Performance: API Login Mất ~3.4 Giây

## 📊 Tóm Tắt

API `/api/auth/login` đang mất **~3.4 giây** để xử lý, đây là **chậm bất thường** cho một API login cơ bản.

**Chuẩn thực tế:**
- Login API tốt: **100-300ms**
- Có external call: 300-800ms
- **> 1 giây**: ⚠️ Bắt đầu đáng lo
- **~3.4 giây**: ❌ **Chậm rõ ràng**

---

## 🔎 Các Điểm Có Thể Gây Chậm (Đã Phân Tích Code)

### 1️⃣ **Database Query - Nghi Vấn Cao** 🔴

**Vấn đề tìm thấy:**

```csharp
// ❌ TRƯỚC (UserRepository.cs - line 19)
public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
{
    var result = _context.Users.FirstOrDefaultAsync(
        u => u.NormalizedUserName == userName.ToUpper(), ct);
    return result;
}
```

**Vấn đề:**
- `userName.ToUpper()` được gọi **trong LINQ expression**
- Oracle/EF Core có thể **không tối ưu** được query này
- Có thể **không dùng index** trên `NORMALIZED_USER_NAME`
- **Thiếu `AsNoTracking()`** → EF Core track entity không cần thiết

**✅ Đã fix:**
```csharp
// ✅ SAU
public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
{
    var normalizedUserName = userName.ToUpperInvariant();
    var result = _context.Users
        .AsNoTracking() // Read-only, không track
        .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);
    return result;
}
```

**Tác động:**
- Normalize **trước khi query** → Oracle có thể dùng index
- `AsNoTracking()` → giảm overhead tracking
- **Ước tính cải thiện: 500ms - 2s** (tùy DB size và index)

---

### 2️⃣ **Password Verification - Nghi Vấn Trung Bình** 🟡

**Code hiện tại:**
```csharp
var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
```

**Phân tích:**
- Dùng `PasswordHasher<User>` mặc định của **ASP.NET Identity**
- Mặc định dùng **PBKDF2** với **10,000 iterations**
- Mỗi lần verify có thể mất **100-500ms** (tùy CPU)

**Đây là bình thường** cho security, nhưng:
- Nếu cost factor quá cao → có thể chậm
- Nếu CPU yếu → chậm hơn

**Giải pháp (nếu cần):**
- Giảm iterations (không khuyến khích - giảm security)
- Dùng async password hasher (nếu có)
- Cache kết quả (không khuyến khích - security risk)

**Ước tính:** 100-500ms (bình thường cho PBKDF2)

---

### 3️⃣ **Oracle Database Connection - Nghi Vấn Trung Bình** 🟡

**Cấu hình hiện tại:**
```csharp
ConnectionTimeout = 15
Pooling = true
ValidateConnection = true
```

**Vấn đề có thể:**
- **Cold start**: Lần đầu connect có thể chậm (1-2s)
- **Connection pool chưa warm**: Nếu app mới start
- **Oracle wallet overhead**: Đọc wallet có thể chậm
- **Network latency**: Nếu DB ở xa (nhưng localhost thì không)

**Giải pháp:**
- Warm up connection pool khi app start
- Tăng connection pool size
- Kiểm tra Oracle wallet path có đúng không

**Ước tính:** 0-2000ms (tùy cold start)

---

### 4️⃣ **JWT Token Generation - Không Phải Vấn Đề** ✅

**Code:**
```csharp
var token = _tokenService.GenerateToken(_jwtSettings, claims);
```

**Phân tích:**
- Dùng **SymmetricSecurityKey** (HMAC-SHA256)
- Rất nhanh, chỉ mất **< 10ms**
- Không phải RSA (RSA có thể chậm hơn)

**Kết luận:** Không phải nguyên nhân chậm

---

### 5️⃣ **Middleware Logging - Không Phải Vấn Đề** ✅

**Phân tích:**
- `GlobalLoggingMiddleware` có capture body
- Nhưng `EnableRequestBodyLogging = false` (mặc định)
- Log cho thấy `RequestBody = null`, `ResponseBody = null`
- Middleware chỉ serialize `LogEntry` object nhỏ

**Kết luận:** Không phải nguyên nhân chậm

---

## 🛠️ Giải Pháp Đã Áp Dụng

### ✅ 1. Thêm Timing Logs Chi Tiết

Đã thêm timing logs vào `AuthService.LoginAsync` để đo từng bước:

```csharp
[LoginAsync] Database query took {ElapsedMs} ms
[LoginAsync] Password verification took {ElapsedMs} ms
[LoginAsync] Building claims took {ElapsedMs} ms
[LoginAsync] JWT token generation took {ElapsedMs} ms
[LoginAsync] Total login processing took {ElapsedMs} ms
```

**Cách dùng:**
1. Chạy lại login request
2. Xem logs để biết **chính xác bước nào chậm**
3. Tập trung optimize bước đó

---

### ✅ 2. Fix Database Query

- Normalize input trước khi query
- Thêm `AsNoTracking()` cho read-only query
- Đảm bảo Oracle có thể dùng index

---

## 📈 Các Bước Tiếp Theo (Nếu Vẫn Chậm)

### 1. Kiểm Tra Database Index

**Kiểm tra xem có index trên `NORMALIZED_USER_NAME` không:**

```sql
-- Oracle
SELECT index_name, column_name 
FROM user_ind_columns 
WHERE table_name = 'USERS' 
  AND column_name = 'NORMALIZED_USER_NAME';
```

**Nếu không có index, tạo:**
```sql
CREATE INDEX idx_users_normalized_username 
ON USERS(NORMALIZED_USER_NAME);
```

**Tác động:** Có thể giảm query time từ 1-2s xuống < 50ms

---

### 2. Warm Up Connection Pool

Thêm vào `Program.cs` hoặc startup:

```csharp
// Warm up DB connection
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
await dbContext.Database.CanConnectAsync();
```

**Tác động:** Giảm cold start time

---

### 3. Kiểm Tra Password Hash Algorithm

Nếu password verification vẫn chậm (> 500ms):

```csharp
// Log password hash format
_logger.LogInformation("Password hash format: {Hash}", 
    user.PasswordHash?.Substring(0, Math.Min(20, user.PasswordHash.Length ?? 0)));
```

- ASP.NET Identity mặc định: `PBKDF2` với 10,000 iterations
- Nếu dùng BCrypt với cost > 12 → có thể chậm

---

### 4. Profile Database Query

Bật SQL logging trong `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Xem query thực tế Oracle chạy và execution plan.

---

## 🎯 Kết Luận

**Nguyên nhân có khả năng cao nhất:**

1. **Database query chậm** (50-70% khả năng)
   - Query không dùng index
   - Cold connection
   - Normalize trong LINQ

2. **Password verification** (20-30% khả năng)
   - PBKDF2 với 10k iterations
   - CPU-bound operation

3. **Oracle connection overhead** (10-20% khả năng)
   - Cold start
   - Wallet path

**Sau khi thêm timing logs, bạn sẽ biết chính xác bước nào chậm!**

---

## 📝 Checklist Sau Khi Chạy Lại

- [ ] Xem logs `[LoginAsync]` để biết timing từng bước
- [ ] Nếu DB query > 500ms → kiểm tra index
- [ ] Nếu password verify > 500ms → đây là bình thường (security)
- [ ] Nếu token generation > 50ms → có vấn đề
- [ ] So sánh timing trước/sau fix database query

---

**Tạo bởi:** Performance Analysis  
**Ngày:** 2026-01-15  
**Version:** 1.0
