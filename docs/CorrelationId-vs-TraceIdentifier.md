# CorrelationId vs TraceIdentifier - Best Practices

## 🎯 TL;DR

> **❌ KHÔNG ghi đè `context.TraceIdentifier`**
> 
> **✅ Dùng `X-Correlation-Id` header + LogContext**

---

## 📊 So sánh

| Aspect | TraceIdentifier | CorrelationId (Custom) |
|--------|----------------|------------------------|
| **Nguồn gốc** | ASP.NET Core Framework | Custom implementation |
| **Scope** | Per-service (thay đổi mỗi service) | Cross-service (giữ nguyên) |
| **Mục đích** | Framework logging | Business tracing |
| **Control** | Framework quản lý | Developer quản lý |
| **Propagation** | ❌ KHÔNG tự động | ✅ Qua header |
| **Best practice** | Giữ nguyên | Push to LogContext |

---

## ❌ Antipattern: Ghi đè TraceIdentifier

### Code SAI

```csharp
public async Task Invoke(HttpContext context)
{
    var correlationId = GenerateOrReadCorrelationId();
    
    // ❌ ANTIPATTERN: Ghi đè framework property
    context.TraceIdentifier = correlationId;
    
    await _next(context);
}
```

### Vấn đề

1. **Mất ý nghĩa gốc**
   - TraceIdentifier có ý nghĩa riêng của framework
   - Ghi đè = làm mất thông tin framework

2. **Confusing**
   - Không rõ đâu là framework value
   - Không rõ đâu là business value

3. **Không cần thiết**
   - Đã có LogContext
   - Đã có header propagation

4. **Breaking expectations**
   - Library/middleware khác có thể rely on TraceIdentifier
   - Behavior không như expected

---

## ✅ Best Practice: Separation of Concerns

### Code ĐÚNG

```csharp
public async Task Invoke(HttpContext context)
{
    // 1. Read or generate CorrelationId
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
        ? headerValue.ToString()
        : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

    // 2. Set header for propagation (request + response)
    context.Request.Headers["X-Correlation-Id"] = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        return Task.CompletedTask;
    });

    // 3. Push to LogContext for automatic enrichment
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await _next(context);
    }
    
    // ✅ TraceIdentifier giữ nguyên giá trị framework
}
```

### Lợi ích

- ✅ **TraceIdentifier**: Giữ nguyên ý nghĩa framework
- ✅ **CorrelationId**: Custom business tracing
- ✅ **Clear separation**: Không nhầm lẫn
- ✅ **Standard compliant**: Header-based propagation

---

## 🔍 Khi nào dùng cái gì?

### TraceIdentifier

**Use case:**
- Framework internal logging
- ASP.NET Core diagnostics
- Local request tracking (within one service)

**Example:**
```csharp
// Framework logging
_logger.LogInformation("Request {TraceId} started", context.TraceIdentifier);
```

### CorrelationId (X-Correlation-Id)

**Use case:**
- Cross-service tracing
- Business logging
- End-to-end request journey
- Production debugging

**Example:**
```csharp
// Business logging (auto-enriched)
_logger.LogInformation("Creating branch {BranchName}", dto.Name);
// → Output: { "message": "Creating branch...", "CorrelationId": "abc-123" }
```

---

## 🌐 Multi-Service Flow

### TraceIdentifier (Per-Service)

```
Client
  ↓
Gateway
  TraceIdentifier: 0HMAAAA:00000001 (framework-generated)
  ↓
BranchService
  TraceIdentifier: 0HMBBBB:00000001 (NEW, framework-generated)
  ↓
AccountService
  TraceIdentifier: 0HMCCCC:00000001 (NEW, framework-generated)

→ Cannot trace full journey ❌
```

### CorrelationId (Cross-Service)

```
Client
  ↓ X-Correlation-Id: abc-123
Gateway
  CorrelationId: abc-123 (read from header)
  TraceIdentifier: 0HMAAAA:00000001 (framework, separate)
  ↓ X-Correlation-Id: abc-123 (propagate)
BranchService
  CorrelationId: abc-123 (SAME, read from header)
  TraceIdentifier: 0HMBBBB:00000001 (framework, separate)
  ↓ X-Correlation-Id: abc-123 (propagate)
AccountService
  CorrelationId: abc-123 (SAME, read from header)
  TraceIdentifier: 0HMCCCC:00000001 (framework, separate)

→ Query Elasticsearch by "abc-123" = Full journey ✅
```

---

## 💡 Accessing CorrelationId

### ❌ BAD: Via TraceIdentifier (if overridden)

```csharp
var correlationId = httpContext.TraceIdentifier;  // ← Confusing, antipattern
```

### ✅ GOOD: Via Header

```csharp
// Option 1: Direct access
var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();

// Option 2: Service abstraction (recommended)
public interface ICorrelationIdAccessor
{
    string GetCorrelationId();
}

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?
            .Request.Headers["X-Correlation-Id"].ToString()
            ?? string.Empty;
    }
}
```

---

## 🎓 Industry Standards

### W3C Trace Context (OpenTelemetry)

```
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
              └─ TraceId (distributed)         └─ SpanId (per-service)
```

- **TraceId**: Cross-service (like CorrelationId)
- **SpanId**: Per-service (like TraceIdentifier)

### HTTP Headers (Manual)

```
X-Correlation-Id: abc-123          ← Custom, cross-service
X-Request-Id: xyz-789              ← Alternative name
```

---

## 📋 Checklist

### ✅ DO

- ✅ Use `X-Correlation-Id` header for cross-service tracing
- ✅ Push to LogContext for automatic enrichment
- ✅ Propagate header to downstream services
- ✅ Keep TraceIdentifier as framework value
- ✅ Use Activity.TraceId if using OpenTelemetry

### ❌ DON'T

- ❌ Override `context.TraceIdentifier`
- ❌ Mix framework and business concepts
- ❌ Rely on TraceIdentifier for cross-service tracing
- ❌ Forget to propagate header downstream

---

## 🚀 Migration Guide

### If you currently override TraceIdentifier

**Step 1: Remove override**

```diff
public async Task Invoke(HttpContext context)
{
    var correlationId = GenerateOrReadCorrelationId();
-   context.TraceIdentifier = correlationId;  // ← Remove this
    
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await _next(context);
    }
}
```

**Step 2: Update access patterns**

```diff
- var correlationId = context.TraceIdentifier;
+ var correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
```

**Step 3: Verify logs**

```bash
# Before: CorrelationId might be in different fields
# After: CorrelationId always in same field (enriched by LogContext)
```

---

## 🎯 Summary

| Question | Answer |
|----------|--------|
| Should I override TraceIdentifier? | ❌ NO |
| What should I use for cross-service tracing? | ✅ X-Correlation-Id header |
| How to enrich logs? | ✅ LogContext.PushProperty |
| Can I use TraceIdentifier? | ✅ YES, for framework logging only |
| What about Activity.TraceId? | ✅ Best for OTel, fallback for CorrelationId |

---

**Key Takeaway:**

> 🔑 **TraceIdentifier = Framework concern (per-service)**
> 
> 🔑 **CorrelationId = Business concern (cross-service)**
> 
> 🔑 **Keep them SEPARATE**

---

**Date**: 2025-01-03
**Status**: ✅ Best Practice Established

