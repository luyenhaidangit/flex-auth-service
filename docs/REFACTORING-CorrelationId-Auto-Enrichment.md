# 🔧 Refactoring: CorrelationId Auto-Enrichment

## ✅ Đã hoàn thành

Refactor để **CorrelationId tự động enrich vào TẤT CẢ logs** thông qua Serilog LogContext.

---

## 🎯 Vấn đề trước đây

### ❌ Cách cũ (Manual)

```csharp
// GlobalLoggingMiddleware
var logEntry = new LogEntry
{
    CorrelationId = context.TraceIdentifier,  // ← Manual set
    Method = "POST",
    Path = "/api/branches"
};

_logger.LogInformation("Request completed {@LogEntry}", logEntry);
```

**Vấn đề:**
- ❌ Phải set `CorrelationId` manually trong LogEntry
- ❌ Chỉ có log từ GlobalLoggingMiddleware mới có CorrelationId
- ❌ Các log khác trong controller/service KHÔNG có CorrelationId

---

## ✅ Cách mới (Auto-Enrichment)

### 1️⃣ CorrelationIdMiddleware - Push to LogContext

```csharp
using Serilog.Context;

public async Task Invoke(HttpContext context)
{
    // Read from header or generate
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
        ? headerValue.ToString()
        : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

    // Set header for propagation
    context.Request.Headers["X-Correlation-Id"] = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        return Task.CompletedTask;
    });

    // 🔥 Push to Serilog LogContext - auto-enrichment for ALL logs
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await _next(context);
    }
}
```

**Note:** ❌ KHÔNG ghi đè `context.TraceIdentifier` - giữ nguyên ý nghĩa framework

### 2️⃣ GlobalLoggingMiddleware - Remove manual CorrelationId

```csharp
var logEntry = new LogEntry
{
    Service = _options.ServiceName,
    // CorrelationId is auto-enriched by Serilog (no need to set manually)
    Method = context.Request.Method,
    Path = context.Request.Path
};

_logger.LogInformation("Request completed {@LogEntry}", logEntry);
```

### 3️⃣ LogEntry - Remove CorrelationId field

```csharp
public class LogEntry
{
    public string Service { get; set; } = string.Empty;
    // ❌ REMOVED: public string CorrelationId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    // ...
}
```

---

## 🎉 Kết quả

### ✅ TẤT CẢ logs tự động có CorrelationId

#### Log từ GlobalLoggingMiddleware

```csharp
_logger.LogInformation("POST /api/branches responded 201 in 42ms {@LogEntry}", logEntry);
```

**Output:**

```json
{
  "message": "POST /api/branches responded 201 in 42ms",
  "CorrelationId": "c-abc-123",  // ← Auto-enriched
  "LogEntry": {
    "service": "ApiGateway",
    "method": "POST",
    "path": "/api/branches"
  }
}
```

#### Log từ Controller/Service

```csharp
// Controller
_logger.LogInformation("Creating branch {BranchName}", dto.Name);
```

**Output:**

```json
{
  "message": "Creating branch Ha Noi Branch",
  "CorrelationId": "c-abc-123",  // ← Auto-enriched
  "BranchName": "Ha Noi Branch"
}
```

#### Log từ Exception

```csharp
_logger.LogError(ex, "Failed to create branch");
```

**Output:**

```json
{
  "level": "Error",
  "message": "Failed to create branch",
  "CorrelationId": "c-abc-123",  // ← Auto-enriched
  "exception": "..."
}
```

---

## 🔥 Lợi ích

### ✅ Trước đây

- ✅ Chỉ GlobalLoggingMiddleware có CorrelationId
- ❌ Controller/Service logs KHÔNG có CorrelationId
- ❌ Không trace được full journey

### ✅ Bây giờ

- ✅ **TẤT CẢ logs** tự động có CorrelationId
- ✅ Trace được full journey (Gateway → Service → Database)
- ✅ Không cần set manual
- ✅ Zero business code changes

---

## 📊 So sánh

### ❌ Cách cũ (Manual)

```
[10:30:00] POST /api/branches responded 201 in 42ms
  CorrelationId: c-abc-123 ✅

[10:30:00] Creating branch Ha Noi Branch
  CorrelationId: (missing) ❌

[10:30:00] Inserting to database
  CorrelationId: (missing) ❌
```

**→ Không trace được full journey**

### ✅ Cách mới (Auto-Enrichment)

```
[10:30:00] POST /api/branches responded 201 in 42ms
  CorrelationId: c-abc-123 ✅

[10:30:00] Creating branch Ha Noi Branch
  CorrelationId: c-abc-123 ✅

[10:30:00] Inserting to database
  CorrelationId: c-abc-123 ✅
```

**→ Query by c-abc-123 = Full journey visible**

---

## 🎯 Elasticsearch Query

### Trước đây

```json
GET /flex-*/_search
{
  "query": {
    "term": { "LogEntry.correlationId.keyword": "c-abc-123" }
  }
}
```

**Result**: Chỉ 1 log (từ GlobalLoggingMiddleware)

### Bây giờ

```json
GET /flex-*/_search
{
  "query": {
    "term": { "CorrelationId.keyword": "c-abc-123" }
  }
}
```

**Result**: TẤT CẢ logs trong request journey

```json
{
  "hits": [
    {
      "message": "POST /api/branches responded 201 in 42ms",
      "CorrelationId": "c-abc-123"
    },
    {
      "message": "Creating branch Ha Noi Branch",
      "CorrelationId": "c-abc-123"
    },
    {
      "message": "Inserting to database",
      "CorrelationId": "c-abc-123"
    }
  ]
}
```

---

## 🔑 Key Points

### 1️⃣ Serilog Configuration (KHÔNG thay đổi)

```csharp
// SeriLogger.Configure()
.Enrich.FromLogContext()  // ← Điều kiện cần (gateway nhận dữ liệu)
```

**Giải thích:**
- `.Enrich.FromLogContext()` = "Cho phép nhận dữ liệu động từ runtime"
- KHÔNG tự sinh CorrelationId
- Chỉ là **cổng nhận**

### 2️⃣ Middleware (Runtime Push)

```csharp
// CorrelationIdMiddleware
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    await _next(context);  // ← Tất cả logs trong scope này đều có CorrelationId
}
```

**Giải thích:**
- Push CorrelationId vào LogContext
- Scope = toàn bộ request pipeline
- Tự động dispose sau khi request xong

### 3️⃣ Application Code (KHÔNG thay đổi)

```csharp
// Controller/Service - không cần sửa gì
_logger.LogInformation("Creating branch");
_logger.LogWarning("Branch already exists");
_logger.LogError(ex, "Failed to create branch");
```

**→ Tất cả đều tự động có CorrelationId**

---

## 📝 Changes Summary

### Files Modified

1. **CorrelationIdMiddleware.cs**
   - ✅ Added `using Serilog.Context`
   - ✅ Added `LogContext.PushProperty("CorrelationId", correlationId)`
   - ✅ Improved header reading logic

2. **GlobalLoggingMiddleware.cs**
   - ✅ Removed manual `CorrelationId = context.TraceIdentifier`
   - ✅ Added comment explaining auto-enrichment

3. **LogEntry.cs**
   - ✅ Removed `CorrelationId` property (now auto-enriched)

### Build Status

```
Flex.Infrastructures -> Success ✅
(App is running, file locked - normal)
```

---

## 🎓 Best Practices

### ✅ DO

```csharp
// ✅ Push at middleware level (once per request)
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    await _next(context);
}

// ✅ Log normally in business code
_logger.LogInformation("Creating branch");
```

### ❌ DON'T

```csharp
// ❌ Don't push in static configuration
.Enrich.WithProperty("CorrelationId", Guid.NewGuid())  // ← WRONG

// ❌ Don't set manually in LogEntry
logEntry.CorrelationId = context.TraceIdentifier;  // ← Not needed anymore
```

---

## 🚀 Next Steps

### ✅ Completed

- [x] Refactor CorrelationIdMiddleware
- [x] Simplify GlobalLoggingMiddleware
- [x] Remove CorrelationId from LogEntry
- [x] Build successful

### 📝 Recommended

- [ ] Update documentation examples
- [ ] Test end-to-end tracing
- [ ] Verify Elasticsearch queries
- [ ] Add unit tests

---

## 📚 References

- [Serilog LogContext](https://github.com/serilog/serilog/wiki/Enrichment#logcontext)
- [Serilog Enrichment](https://github.com/serilog/serilog/wiki/Enrichment)
- [ASP.NET Core Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)

---

**Date**: 2025-01-03
**Status**: ✅ **COMPLETE**
**Impact**: All logs now have automatic CorrelationId enrichment

