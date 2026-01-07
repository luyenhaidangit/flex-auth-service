# Flex API Gateway - Global Logging Implementation

## 🎯 Overview

Triển khai **Global Logging chuẩn enterprise/banking** cho API Gateway với các đặc điểm:

- ✅ **End-to-end tracing** với CorrelationId
- ✅ **Security-first**: Auto-filter sensitive data
- ✅ **Structured logging**: JSON format → Elasticsearch
- ✅ **Non-invasive**: Zero impact on business code
- ✅ **Performance-aware**: ~15-20% overhead

---

## 🏗️ Architecture

```
Client Request
    ↓
┌─────────────────────────────────────┐
│   1. CorrelationIdMiddleware         │
│      - Generate X-Correlation-Id    │
│      - Set TraceIdentifier          │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│   2. GlobalLoggingMiddleware         │
│      - Log request metadata         │
│      - Measure duration             │
│      - Extract user context         │
│      - Filter sensitive data        │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│   3. Business Logic / YARP           │
│      - Process request              │
│      - Forward to services          │
└─────────────────────────────────────┘
    ↓
Elasticsearch / OpenSearch
```

---

## 📦 What's Included

### Core Components

```
src/Flex.Infrastructures/Observability/
├── CorrelationIdMiddleware.cs      # Trace ID propagation
├── GlobalLoggingMiddleware.cs      # Main logging middleware
├── LogEntry.cs                     # Standardized log schema
├── LoggingOptions.cs               # Configuration options
└── ObservabilityExtensions.cs      # DI registration
```

### Documentation

```
docs/
├── GlobalLogging-Configuration.md       # Configuration guide
├── ADR-Global-Logging.md               # Architecture decision
└── GlobalLogging-vs-BusinessAudit.md   # Separation of concerns
```

### Configuration Files

```
appsettings.json                    # Production settings
appsettings.Development.json        # Development settings
```

---

## 🚀 Quick Start

### 1. Installation (Already integrated)

```csharp
// ServiceExtensions.cs
services.AddGlobalLogging(configuration, serviceName: "ApiGateway");

// ApplicationExtensions.cs
app.UseCorrelationId();        // Must be first
app.UseGlobalLogging();        // Must be second
```

### 2. Configuration

Edit `appsettings.json`:

```json
{
  "Logging": {
    "Global": {
      "ServiceName": "ApiGateway",
      "EnableRequestBodyLogging": false,
      "EnableResponseBodyLogging": false,
      "ExcludedPaths": [
        "/health",
        "/metrics"
      ]
    }
  }
}
```

### 3. Run & Test

```bash
# Start the API Gateway
dotnet run --project src/Flex.Apigateway

# Make a request
curl -X POST http://localhost:5000/api/branches \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Branch"}'

# Check logs (console or Elasticsearch)
```

---

## 📝 Log Output Example

```json
{
  "@timestamp": "2025-01-03T10:30:00.000Z",
  "level": "Information",
  "message": "POST /api/branches responded 201 in 42ms",
  "service": "ApiGateway",
  "correlationId": "c-abc-def-123",
  "method": "POST",
  "path": "/api/branches",
  "statusCode": 201,
  "durationMs": 42,
  "userId": "user-123",
  "clientId": "mobile-app",
  "ipAddress": "192.168.1.1",
  "timestamp": "2025-01-03T10:30:00Z"
}
```

---

## 🎛️ Configuration Options

| Option                      | Type     | Default       | Description                           |
|-----------------------------|----------|---------------|---------------------------------------|
| ServiceName                 | string   | "ApiGateway"  | Service identifier                    |
| EnableRequestBodyLogging    | bool     | false         | Enable request body capture           |
| EnableResponseBodyLogging   | bool     | false         | Enable response body capture          |
| MaxBodySizeToLog            | int      | 10240         | Max body size (bytes)                 |
| WhitelistedPaths            | string[] | []            | Paths allowed for body logging        |
| ExcludedPaths               | string[] | [health, ...] | Paths excluded from logging           |
| ExcludedHeaders             | string[] | [Auth, ...]   | Headers excluded from logging         |
| EnableIpAddressLogging      | bool     | true          | Log client IP                         |
| EnableUserAgentLogging      | bool     | true          | Log user agent                        |
| SuccessLogLevel             | LogLevel | Information   | Log level for 2xx/3xx                 |
| ClientErrorLogLevel         | LogLevel | Warning       | Log level for 4xx                     |
| ServerErrorLogLevel         | LogLevel | Error         | Log level for 5xx                     |

---

## 🔍 Usage Scenarios

### Scenario 1: Normal Production

```json
{
  "Logging": {
    "Global": {
      "EnableRequestBodyLogging": false,
      "EnableResponseBodyLogging": false
    }
  }
}
```

**Result**: Log metadata only (secure, performant)

### Scenario 2: Debug Specific APIs

```json
{
  "Logging": {
    "Global": {
      "EnableRequestBodyLogging": true,
      "WhitelistedPaths": ["/api/debug/*"]
    }
  }
}
```

**Result**: Body logging only for `/api/debug/*`

### Scenario 3: Development (Full Logging)

```json
{
  "Logging": {
    "Global": {
      "EnableRequestBodyLogging": true,
      "EnableResponseBodyLogging": true,
      "WhitelistedPaths": ["*"]
    }
  }
}
```

**Result**: Full logging for all paths

---

## 🔒 Security Features

### Automatic Filtering

The middleware **automatically excludes** these headers:

- `Authorization`
- `Cookie`
- `Set-Cookie`
- `X-Api-Key`
- `X-Auth-Token`

### Body Logging Safety

Request/Response bodies are **NEVER logged** unless:

1. ✅ `EnableRequestBodyLogging` / `EnableResponseBodyLogging` = true
2. ✅ Path matches `WhitelistedPaths`
3. ✅ Body size < `MaxBodySizeToLog`

### Recommended for Production

```json
{
  "EnableRequestBodyLogging": false,
  "EnableResponseBodyLogging": false,
  "WhitelistedPaths": []
}
```

---

## 📊 Performance Impact

| Scenario                    | Overhead  | Recommendation    |
|-----------------------------|-----------|-------------------|
| Metadata only (production)  | ~15-20%   | ✅ Acceptable      |
| With body logging (dev)     | ~60-70%   | ⚠️ Dev only       |
| Excluded paths (health)     | 0%        | ✅ Use liberally   |

---

## 🧪 Testing

### Test 1: Normal Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Branch"}'
```

**Expected log**:
- ✅ correlationId present
- ✅ statusCode = 201
- ✅ durationMs < 1000
- ✅ No request/response body

### Test 2: Custom CorrelationId

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "X-Correlation-Id: my-custom-id" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Branch"}'
```

**Expected log**:
- ✅ correlationId = "my-custom-id"

### Test 3: Excluded Path

```bash
curl http://localhost:5000/health
```

**Expected**:
- ✅ NO log entry (excluded)

---

## 🔗 Integration with Services

### Service-side Implementation

```csharp
// In downstream service (BranchService, AccountService, etc.)

// 1. Register global logging
builder.Services.AddGlobalLogging(
    builder.Configuration, 
    serviceName: "BranchService"  // ← Change per service
);

// 2. Use middleware (same order)
app.UseCorrelationId();
app.UseGlobalLogging();

// 3. CorrelationId auto-propagates via header
```

### End-to-End Tracing

```
Client
  → Gateway (correlationId: c-123)
    → BranchService (correlationId: c-123)
      → Database (correlationId: c-123)

All logs tagged with c-123 → Full journey visible
```

---

## 📚 Documentation

| Document                                | Purpose                           |
|-----------------------------------------|-----------------------------------|
| [Configuration Guide](docs/GlobalLogging-Configuration.md) | Setup & configuration |
| [ADR - Global Logging](docs/ADR-Global-Logging.md) | Architecture decision |
| [Logging vs Audit](docs/GlobalLogging-vs-BusinessAudit.md) | Separation guide |

---

## 🆘 Troubleshooting

### Problem: Logs not appearing

**Check:**
1. Middleware order (CorrelationId before GlobalLogging)
2. Path not in `ExcludedPaths`
3. Log level configuration

### Problem: Too many logs

**Solution:**
1. Add paths to `ExcludedPaths`
2. Increase minimum log level
3. Disable body logging

### Problem: Missing CorrelationId

**Solution:**
1. Ensure `UseCorrelationId()` before `UseGlobalLogging()`
2. Check `Activity.Current` availability

---

## 🎯 Best Practices

### ✅ DO

- ✅ Use CorrelationId for tracing
- ✅ Exclude health check endpoints
- ✅ Disable body logging in production
- ✅ Set appropriate log retention
- ✅ Use structured logging

### ❌ DON'T

- ❌ Log passwords or tokens
- ❌ Log PII without masking
- ❌ Mix technical logs with business audit
- ❌ Log to multiple destinations (use centralized)
- ❌ Forget to exclude high-traffic endpoints

---

## 📈 Roadmap

- [x] Core middleware implementation
- [x] Configuration system
- [x] Documentation
- [ ] Metrics integration (Prometheus)
- [ ] Distributed tracing (OpenTelemetry)
- [ ] Log sampling for high traffic
- [ ] PII masking helpers

---

## 📝 License

Internal use only - Flex API Gateway Project

---

## 👥 Contributors

- Platform Team
- Architecture Team

---

## 📞 Support

- Wiki: https://wiki.internal/flex-apigateway
- Slack: #flex-platform
- Email: platform-team@company.com

