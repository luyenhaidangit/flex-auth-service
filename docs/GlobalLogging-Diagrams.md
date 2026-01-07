# Global Logging Architecture Diagrams

## 📐 Overview

Tài liệu này cung cấp các sơ đồ kiến trúc cho Global Logging system.

---

## 🏗️ Component Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                        Client / Mobile App                      │
└────────────────────────────────────────────────────────────────┘
                                │
                                │ HTTP Request
                                │ Header: X-Correlation-Id (optional)
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                         API Gateway                             │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  1. CorrelationIdMiddleware                           │    │
│  │     ┌────────────────────────────────────────────┐    │    │
│  │     │ • Read X-Correlation-Id from header        │    │    │
│  │     │ • Generate if missing (Activity.TraceId)   │    │    │
│  │     │ • Set context.TraceIdentifier              │    │    │
│  │     │ • Add to Response header                   │    │    │
│  │     └────────────────────────────────────────────┘    │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  2. GlobalLoggingMiddleware                           │    │
│  │     ┌────────────────────────────────────────────┐    │    │
│  │     │ • Capture request metadata                 │    │    │
│  │     │   - Method, Path, Headers                  │    │    │
│  │     │   - UserId, ClientId, IP                   │    │    │
│  │     │ • Start timer (Stopwatch)                  │    │    │
│  │     │ • Enable request buffering (if needed)     │    │    │
│  │     │ • Replace response stream (capture)        │    │    │
│  │     └────────────────────────────────────────────┘    │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  3. RequestGuardMiddleware                            │    │
│  │     • Request validation                              │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  4. ExceptionHandlingMiddleware                       │    │
│  │     • Catch unhandled exceptions                      │    │
│  │     • Log with CorrelationId                          │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  5. Authentication & Authorization                    │    │
│  │     • JWT validation                                  │    │
│  │     • Extract user claims                             │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  6. YARP Reverse Proxy                                │    │
│  │     • Forward request to backend service              │    │
│  │     • Propagate X-Correlation-Id header               │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  7. GlobalLoggingMiddleware (Response)                │    │
│  │     ┌────────────────────────────────────────────┐    │    │
│  │     │ • Stop timer                               │    │    │
│  │     │ • Capture response status                  │    │    │
│  │     │ • Capture response body (if whitelisted)   │    │    │
│  │     │ • Build LogEntry                           │    │    │
│  │     │ • Log to Serilog                           │    │    │
│  │     └────────────────────────────────────────────┘    │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│                      Response to Client                         │
│                      Header: X-Correlation-Id                   │
└────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                         Serilog Pipeline                        │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  Enrichers                                            │    │
│  │  • FromLogContext                                     │    │
│  │  • MachineName                                        │    │
│  │  • Environment                                        │    │
│  │  • Application                                        │    │
│  └───────────────────────────────────────────────────────┘    │
│                            ↓                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │  Sinks (Parallel)                                     │    │
│  │  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │    │
│  │  │  Console    │  │  File        │  │ Elastic    │  │    │
│  │  │  (Dev)      │  │  (Temp 7d)   │  │ (30d TTL)  │  │    │
│  │  └─────────────┘  └──────────────┘  └────────────┘  │    │
│  └───────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                    Elasticsearch / OpenSearch                   │
│                                                                 │
│  Index: flex-apigateway-{YYYY-MM}                              │
│  Retention: 30 days (TTL)                                      │
│  Replicas: 1                                                   │
│  Shards: 2                                                     │
└────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Request Flow Sequence

```
┌────────┐         ┌─────────┐         ┌──────────┐         ┌────────┐
│ Client │         │ Gateway │         │ Service  │         │   ELK  │
└───┬────┘         └────┬────┘         └────┬─────┘         └───┬────┘
    │                   │                   │                    │
    │ POST /branches    │                   │                    │
    │ (no correlation)  │                   │                    │
    ├──────────────────>│                   │                    │
    │                   │                   │                    │
    │                   │ [1] Generate      │                    │
    │                   │ correlationId     │                    │
    │                   │ = c-abc-123       │                    │
    │                   │                   │                    │
    │                   │ [2] Start timer   │                    │
    │                   │     t0 = 0ms      │                    │
    │                   │                   │                    │
    │                   │ [3] Forward       │                    │
    │                   │ + X-Correlation-Id│                    │
    │                   ├──────────────────>│                    │
    │                   │                   │                    │
    │                   │                   │ [4] Process        │
    │                   │                   │     business       │
    │                   │                   │     logic          │
    │                   │                   │                    │
    │                   │   Response 201    │                    │
    │                   │<──────────────────┤                    │
    │                   │                   │                    │
    │                   │ [5] Stop timer    │                    │
    │                   │     t1 = 42ms     │                    │
    │                   │                   │                    │
    │                   │ [6] Build LogEntry│                    │
    │                   │     {             │                    │
    │                   │       correlationId: "c-abc-123",      │
    │                   │       durationMs: 42,                  │
    │                   │       statusCode: 201                  │
    │                   │     }             │                    │
    │                   │                   │                    │
    │                   │ [7] Log async     │                    │
    │                   ├───────────────────┼───────────────────>│
    │                   │                   │                    │
    │   Response 201    │                   │                    │
    │   X-Correlation-Id│                   │                    │
    │<──────────────────┤                   │                    │
    │                   │                   │                    │
```

---

## 📊 Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                     HTTP Request                             │
│  {                                                           │
│    method: "POST",                                           │
│    path: "/api/branches",                                    │
│    headers: {                                                │
│      "Content-Type": "application/json",                     │
│      "X-User-Id": "user-123",                                │
│      "Authorization": "Bearer token..."                      │
│    },                                                        │
│    body: { "name": "Ha Noi Branch" }                         │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────────────────────┐
        │  CorrelationIdMiddleware          │
        │  correlationId = "c-abc-123"      │
        └───────────────────────────────────┘
                            ↓
        ┌───────────────────────────────────┐
        │  GlobalLoggingMiddleware          │
        │  Extract & Filter                 │
        └───────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      LogEntry                                │
│  {                                                           │
│    service: "ApiGateway",                                    │
│    correlationId: "c-abc-123",                               │
│    method: "POST",                                           │
│    path: "/api/branches",                                    │
│    statusCode: 201,                                          │
│    durationMs: 42,                                           │
│    userId: "user-123",                  ← Extracted          │
│    ipAddress: "192.168.1.1",            ← Extracted          │
│    userAgent: "Mozilla/5.0...",         ← Extracted          │
│    timestamp: "2025-01-03T10:30:00Z",                        │
│    requestBody: null,                   ← Filtered (prod)    │
│    responseBody: null                   ← Filtered (prod)    │
│  }                                                           │
│                                                              │
│  ❌ EXCLUDED (Security):                                     │
│    - Authorization header                                    │
│    - Cookie header                                           │
│    - Request/Response body (production)                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
                       Serilog
                            ↓
                    Elasticsearch
```

---

## 🔐 Security Filtering Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   Incoming Headers                           │
│  {                                                           │
│    "Authorization": "Bearer eyJhbGc...",     ← SENSITIVE     │
│    "Cookie": "session=abc123",              ← SENSITIVE     │
│    "X-Api-Key": "secret-key",               ← SENSITIVE     │
│    "X-User-Id": "user-123",                 ← OK            │
│    "X-Client-Id": "mobile-app",             ← OK            │
│    "User-Agent": "Mozilla/5.0..."           ← OK            │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────────────────────┐
        │  Security Filter                  │
        │  Check against ExcludedHeaders    │
        └───────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   Logged Headers                             │
│  {                                                           │
│    "X-User-Id": "user-123",                                  │
│    "X-Client-Id": "mobile-app",                              │
│    "User-Agent": "Mozilla/5.0..."                            │
│  }                                                           │
│                                                              │
│  ❌ EXCLUDED:                                                │
│    - Authorization                                           │
│    - Cookie                                                  │
│    - X-Api-Key                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🌐 Multi-Service Tracing

```
┌──────────────────────────────────────────────────────────────┐
│                         Client                                │
│  Sends: POST /api/branches                                    │
│  Header: (none)                                               │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                      API Gateway                              │
│  [CorrelationIdMiddleware]                                    │
│    ↳ Generate: correlationId = "c-mobile-123"                │
│  [GlobalLoggingMiddleware]                                    │
│    ↳ Log: {                                                   │
│         service: "ApiGateway",                                │
│         correlationId: "c-mobile-123",                        │
│         path: "/api/branches",                                │
│         durationMs: 120                                       │
│       }                                                       │
│  [YARP]                                                       │
│    ↳ Forward to BranchService                                │
│    ↳ Header: X-Correlation-Id: c-mobile-123                  │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                    Branch Service                             │
│  [CorrelationIdMiddleware]                                    │
│    ↳ Read: correlationId = "c-mobile-123"                    │
│  [GlobalLoggingMiddleware]                                    │
│    ↳ Log: {                                                   │
│         service: "BranchService",                             │
│         correlationId: "c-mobile-123",                        │
│         method: "CreateBranch",                               │
│         durationMs: 95                                        │
│       }                                                       │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                    Database / ORM                             │
│  Log (if enabled): {                                          │
│    service: "Database",                                       │
│    correlationId: "c-mobile-123",                             │
│    query: "INSERT INTO Branches...",                          │
│    durationMs: 45                                             │
│  }                                                            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                     Elasticsearch                             │
│  Query: correlationId = "c-mobile-123"                        │
│  Result:                                                      │
│    [                                                          │
│      { service: "ApiGateway", durationMs: 120 },              │
│      { service: "BranchService", durationMs: 95 },            │
│      { service: "Database", durationMs: 45 }                  │
│    ]                                                          │
│  → Full journey visible!                                      │
└──────────────────────────────────────────────────────────────┘
```

---

## 🎛️ Configuration Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                   Default Values                             │
│  (LoggingOptions.cs)                                         │
│  {                                                           │
│    ServiceName: "UnknownService",                            │
│    EnableRequestBodyLogging: false,                          │
│    EnableResponseBodyLogging: false,                         │
│    MaxBodySizeToLog: 10240,                                  │
│    ExcludedPaths: ["/health", "/metrics", ...],              │
│    ...                                                       │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓ (Override)
┌─────────────────────────────────────────────────────────────┐
│             appsettings.json (Base)                          │
│  {                                                           │
│    "Logging": {                                              │
│      "Global": {                                             │
│        "ServiceName": "ApiGateway",                          │
│        "EnableIpAddressLogging": true                        │
│      }                                                       │
│    }                                                         │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓ (Override)
┌─────────────────────────────────────────────────────────────┐
│       appsettings.Development.json                           │
│  {                                                           │
│    "Logging": {                                              │
│      "Global": {                                             │
│        "ServiceName": "ApiGateway-Dev",                      │
│        "EnableRequestBodyLogging": true,                     │
│        "EnableResponseBodyLogging": true,                    │
│        "WhitelistedPaths": ["/api/*"]                        │
│      }                                                       │
│    }                                                         │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓ (Override)
┌─────────────────────────────────────────────────────────────┐
│         Environment Variables (Optional)                     │
│  Logging__Global__ServiceName=ApiGateway-Prod                │
│  Logging__Global__EnableRequestBodyLogging=false             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  Final Configuration                         │
│  Used at runtime by GlobalLoggingMiddleware                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📈 Performance Impact

```
┌─────────────────────────────────────────────────────────────┐
│               Without Global Logging                         │
│                                                              │
│  Client ──────────> Gateway ──────────> Service             │
│          5ms                  10ms                           │
│                                                              │
│  Total: 15ms                                                 │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│          With Global Logging (Metadata Only)                 │
│                                                              │
│  Client ──────────> Gateway ──────────> Service             │
│          5ms         │  2ms   10ms                           │
│                      ↓                                       │
│                   Logging                                    │
│                  (async)                                     │
│                                                              │
│  Total: 17ms (+13% overhead) ✅ Acceptable                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│        With Global Logging (Body Logging Enabled)            │
│                                                              │
│  Client ──────────> Gateway ──────────> Service             │
│          5ms         │  10ms  10ms                           │
│                      ↓                                       │
│                   Logging                                    │
│                 (body capture)                               │
│                                                              │
│  Total: 25ms (+67% overhead) ⚠️ Dev only                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Storage Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Application Logs                            │
│  (GlobalLoggingMiddleware → Serilog)                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────┴──────────────────┐
        │                                       │
        ▼                                       ▼
┌─────────────────┐                  ┌──────────────────────┐
│  Console Sink   │                  │   File Sink          │
│  (Development)  │                  │   logs/log-*.txt     │
│  Immediate      │                  │   Retention: 7 days  │
└─────────────────┘                  │   Size: 10MB/file    │
                                     └──────────────────────┘
                                                │
                                                ▼
                                     ┌──────────────────────┐
                                     │  Elasticsearch Sink  │
                                     │  Index: flex-api-*   │
                                     │  Retention: 30 days  │
                                     │  Replicas: 1         │
                                     │  Shards: 2           │
                                     └──────────────────────┘
                                                │
                                                ▼
                                     ┌──────────────────────┐
                                     │   Index Lifecycle    │
                                     │   Management (ILM)   │
                                     │   - Hot: 7 days      │
                                     │   - Warm: 23 days    │
                                     │   - Delete: 30d+     │
                                     └──────────────────────┘
```

---

## 🎯 Summary

These diagrams illustrate:

1. **Component flow**: How middleware stack processes requests
2. **Sequence flow**: Timing and order of operations
3. **Data transformation**: From HTTP request → LogEntry → Elasticsearch
4. **Security**: How sensitive data is filtered
5. **Tracing**: How CorrelationId propagates across services
6. **Configuration**: How settings are layered
7. **Performance**: Impact on request latency
8. **Storage**: Where and how long logs are kept

All diagrams support the enterprise-grade architecture described in the ADR and configuration guides.

