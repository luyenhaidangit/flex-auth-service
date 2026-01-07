# Production-Ready API Gateway với YARP

## 📋 Tổng quan

Đây là API Gateway production-ready với đầy đủ Resilience patterns và best practices cho môi trường Production.

## ✅ Tính năng đã triển khai

### 1. **Timeout Protection** ⏱️
- **Gateway-level timeout**: 4 giây (configurable)
- **Per-cluster timeout**: Cấu hình trong `yarp.json`
- **Tránh treo connection** khi downstream service chậm

**Config trong `yarp.json`:**
```json
{
  "HttpClient": {
    "RequestTimeout": "00:00:04"
  }
}
```

### 2. **Health Checks** 🏥

#### Active Health Check
- Tự động ping `/health` endpoint của downstream services
- **Interval**: 10 giây
- **Timeout**: 2 giây
- **Policy**: ConsecutiveFailures (loại bỏ destination lỗi liên tục)

#### Passive Health Check
- Theo dõi lỗi thực tế từ traffic
- **Policy**: TransportFailureRate
- **Reactivation Period**: 60 giây

**Config trong `yarp.json`:**
```json
{
  "HealthCheck": {
    "Active": {
      "Enabled": true,
      "Interval": "00:00:10",
      "Timeout": "00:00:02",
      "Policy": "ConsecutiveFailures",
      "Path": "/health"
    },
    "Passive": {
      "Enabled": true,
      "Policy": "TransportFailureRate",
      "ReactivationPeriod": "00:01:00"
    }
  }
}
```

### 3. **Circuit Breaker** 🔌
- **Ngăn cascade failures** khi downstream service lỗi
- **Sampling Duration**: 30 giây
- **Minimum Throughput**: 20 requests
- **Failure Ratio**: 50% (mở circuit nếu >50% requests lỗi)
- **Break Duration**: 20 giây (thời gian circuit mở)

**Implementation:** `ResilienceExtensions.cs`

### 4. **Retry Policy** 🔄
- **Retry tối đa**: 1 lần (production best practice)
- **Delay**: 200ms với Exponential backoff
- **Jitter**: Enabled (tránh thundering herd)
- **Chỉ retry**: NetworkFailures, Timeout, 5xx errors

**Lưu ý:** Gateway không nên retry nhiều để tránh amplifying load!

### 5. **Rate Limiting** 🚦
- **Token Bucket Algorithm**
- **Limit**: 100 requests/second per client
- **Partition by**: User → Client ID → IP Address
- **Queue**: 0 (fail fast)

**Config trong `RateLimitingExtensions.cs`**

### 6. **Correlation ID Propagation** 🔗
- Tự động forward `X-Correlation-Id` từ gateway → downstream services
- Trace requests xuyên suốt microservices
- Auto-enrichment vào logs (Serilog)

**Implementation:**
- `CorrelationIdMiddleware.cs`: Tạo/đọc correlation ID
- `CorrelationIdHandler.cs`: Forward xuống downstream

### 7. **Load Balancing** ⚖️
- **Policy**: PowerOfTwoChoices (performance tốt hơn RoundRobin)
- Tự động loại bỏ destinations không healthy

### 8. **Bulkhead (Concurrency Limiter)** 🛡️
- **Permit Limit**: 200 concurrent requests
- **Queue**: 0 (fail fast)
- Bảo vệ gateway khỏi bị overwhelm

### 9. **Standard Error Responses** ❌
- Gateway-specific errors:
  - `CIRCUIT_BREAKER_OPEN`: Circuit breaker đang mở
  - `GATEWAY_TIMEOUT`: Request timeout
  - `SERVICE_UNAVAILABLE`: Downstream service unavailable
- **Không lộ stack trace** ra ngoài
- Structured JSON response

**Implementation:** `ExceptionHandlingMiddleware.cs`

### 10. **Gateway Health Endpoint** 🩺
- **Endpoint**: `/health`
- Check sức khỏe của gateway (không phải downstream)

---

## 🚀 Cách sử dụng

### 1. Thêm downstream service mới

**Bước 1:** Cập nhật `yarp.json`

```json
{
  "ReverseProxy": {
    "Routes": {
      "approval-api": {
        "ClusterId": "approval-service",
        "Match": {
          "Path": "/api/approvals/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "approval-service": {
        "LoadBalancingPolicy": "PowerOfTwoChoices",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:02",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:05"
        },
        "Destinations": {
          "d1": {
            "Address": "https://approval-svc.internal/",
            "Health": "https://approval-svc.internal/health"
          }
        }
      }
    }
  }
}
```

**Bước 2:** Restart gateway (config sẽ hot-reload nếu có watch)

### 2. Customize timeout cho route cụ thể

Nếu một service cần timeout cao hơn (ví dụ: report service):

```csharp
// Trong ServiceExtensions.cs
services.AddHttpClient("report-client")
    .AddCustomResilience(
        timeout: TimeSpan.FromSeconds(10),
        maxRetryAttempts: 0, // Không retry report
        circuitBreakerFailureRatio: 0.7
    );
```

### 3. Per-route Rate Limiting

Nếu cần rate limit khác nhau cho từng route:

```csharp
// Trong ApplicationExtensions.cs
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseRateLimiter();
});

// Thêm policy riêng
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("heavy-api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(1)
            }));
});
```

### 4. Monitor Circuit Breaker

Để monitor circuit breaker state:

```csharp
// Thêm vào ResilienceExtensions.cs
options.CircuitBreaker.OnOpened = args =>
{
    // Log hoặc emit metric
    logger.LogWarning("Circuit breaker opened for {ServiceName}", serviceName);
    return ValueTask.CompletedTask;
};
```

---

## 📊 Monitoring & Observability

### Metrics cần theo dõi

1. **Rate Limit rejections**: Đếm 429 responses
2. **Circuit Breaker state**: Open/Closed/Half-Open
3. **Timeout rate**: Đếm 504 Gateway Timeout
4. **Downstream health**: Health check failures
5. **Request duration**: P50, P95, P99 latency

### Logs quan trọng

- `CorrelationId`: Track requests xuyên services
- `StatusCode`: HTTP status distribution
- `Duration`: Request processing time
- `ErrorCode`: Gateway-specific error codes

### Serilog Integration

Tất cả logs đã tự động có `CorrelationId` thanks to `CorrelationIdMiddleware`:

```json
{
  "Timestamp": "2026-01-05T10:30:00",
  "Level": "Error",
  "MessageTemplate": "Circuit breaker opened",
  "Properties": {
    "CorrelationId": "abc123",
    "ServiceName": "branch-service"
  }
}
```

---

## 🔒 Security Checklist

- [x] JWT Authentication
- [x] Rate Limiting per client
- [x] HTTPS enforcement
- [x] ForwardedHeaders validation (trusted IPs)
- [x] No stack trace leakage
- [ ] WAF/DDoS protection (cần external service như Cloudflare)
- [ ] API Key validation (nếu cần)
- [ ] CORS policy tùy theo environment

---

## 🎯 Production Deployment Checklist

### Pre-deployment

- [ ] Test health checks với downstream services
- [ ] Validate timeout settings (không quá cao)
- [ ] Test circuit breaker với failure injection
- [ ] Load test với rate limit
- [ ] Verify logs có CorrelationId

### Deployment

- [ ] Deploy gateway trước downstream services
- [ ] Monitor health check status
- [ ] Check circuit breaker không open liên tục
- [ ] Verify 429 rate limit responses
- [ ] Test end-to-end với CorrelationId tracing

### Post-deployment

- [ ] Monitor error rate (nên < 1%)
- [ ] Monitor P99 latency
- [ ] Check circuit breaker metrics
- [ ] Verify health check success rate
- [ ] Tune rate limit nếu cần

---

## 🛠️ Troubleshooting

### Circuit Breaker liên tục Open

**Nguyên nhân:**
- Downstream service thực sự unhealthy
- FailureRatio quá thấp
- MinimumThroughput quá thấp

**Giải pháp:**
1. Check health của downstream service
2. Tăng `FailureRatio` từ 0.5 → 0.7
3. Tăng `MinimumThroughput` từ 20 → 50

### Request bị timeout liên tục

**Nguyên nhân:**
- Downstream service chậm
- Timeout quá thấp
- Database query chậm ở downstream

**Giải pháp:**
1. Profile downstream service
2. Tăng `RequestTimeout` cho cluster cụ thể
3. Optimize downstream performance

### Rate Limit 429 quá nhiều

**Nguyên nhân:**
- Legitimate traffic cao hơn dự kiến
- Bot/crawler attack
- Frontend retry loop

**Giải pháp:**
1. Tăng `TokenLimit` và `TokensPerPeriod`
2. Implement per-user rate limit (cao hơn per-IP)
3. Add bot detection

---

## 🔗 Tài liệu tham khảo

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Polly Resilience](https://www.pollydocs.org/)
- [Rate Limiting in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Circuit Breaker Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)

---

## 📝 Changelog

### v1.0.0 (2026-01-05)
- ✅ Added Microsoft.Extensions.Http.Resilience
- ✅ Implemented Timeout + Retry + Circuit Breaker + Bulkhead
- ✅ Added Active & Passive Health Checks
- ✅ Correlation ID propagation to downstream
- ✅ Production-level Rate Limiting (100 req/s)
- ✅ Gateway-specific error handling
- ✅ Health check endpoint (`/health`)
- ✅ Load balancing with PowerOfTwoChoices

---

## 🎓 Best Practices Summary

1. **Timeout**: Luôn set timeout ở gateway level (3-5s)
2. **Retry**: Tối đa 1 lần, chỉ GET/idempotent
3. **Circuit Breaker**: Bắt buộc để tránh cascade failure
4. **Health Check**: Active + Passive cho high availability
5. **Rate Limit**: Per-user/client, không chỉ per-IP
6. **CorrelationId**: Forward xuống tất cả downstream services
7. **No Fallback**: Gateway không fake data, chỉ trả lỗi chuẩn
8. **Monitoring**: Log metrics quan trọng (circuit state, timeout rate)

