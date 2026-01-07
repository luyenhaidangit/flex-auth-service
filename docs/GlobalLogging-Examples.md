# Global Logging Examples & Samples

## 📋 Overview

Document này cung cấp các ví dụ thực tế về cách Global Logging hoạt động trong các scenario khác nhau.

---

## 🎯 Scenario 1: Normal Request (Production)

### Configuration

```json
{
  "Logging": {
    "Global": {
      "ServiceName": "ApiGateway",
      "EnableRequestBodyLogging": false,
      "EnableResponseBodyLogging": false
    }
  }
}
```

### Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user-123" \
  -H "X-Client-Id: mobile-app" \
  -d '{
    "name": "Ha Noi Branch",
    "code": "HN-001",
    "address": "100 Nguyen Hue St."
  }'
```

### Log Output (Elasticsearch)

```json
{
  "@timestamp": "2025-01-03T10:30:00.123Z",
  "level": "Information",
  "messageTemplate": "POST /api/branches responded 201 in 42ms {@LogEntry}",
  "message": "POST /api/branches responded 201 in 42ms",
  "fields": {
    "service": "ApiGateway",
    "correlationId": "e3f2a1b4-5c6d-7e8f-9a0b-1c2d3e4f5a6b",
    "method": "POST",
    "path": "/api/branches",
    "statusCode": 201,
    "durationMs": 42,
    "userId": "user-123",
    "clientId": "mobile-app",
    "ipAddress": "192.168.1.100",
    "userAgent": "curl/7.68.0",
    "timestamp": "2025-01-03T10:30:00Z",
    "requestBody": null,
    "responseBody": null
  },
  "Environment": "Production",
  "Application": "flex-apigateway",
  "MachineName": "api-server-01"
}
```

### ✅ Observations

- ✅ User ID captured
- ✅ Client ID captured
- ✅ Duration measured
- ✅ NO request/response body (secure)
- ✅ CorrelationId auto-generated

---

## 🎯 Scenario 2: Request with Custom CorrelationId

### Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "X-Correlation-Id: mobile-session-abc-123" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Branch"}'
```

### Log Output

```json
{
  "@timestamp": "2025-01-03T10:31:00.456Z",
  "level": "Information",
  "message": "POST /api/branches responded 201 in 38ms",
  "fields": {
    "correlationId": "mobile-session-abc-123",
    "method": "POST",
    "path": "/api/branches",
    "statusCode": 201,
    "durationMs": 38
  }
}
```

### ✅ Observations

- ✅ Custom CorrelationId preserved
- ✅ Can track request across services

---

## 🎯 Scenario 3: Error Request (500)

### Request

```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "from": "acc-123",
    "to": "invalid",
    "amount": 1000000
  }'
```

### Log Output

```json
{
  "@timestamp": "2025-01-03T10:32:00.789Z",
  "level": "Error",
  "message": "POST /api/transactions responded 500 in 1205ms",
  "fields": {
    "service": "ApiGateway",
    "correlationId": "f1e2d3c4-b5a6-7c8d-9e0f-1a2b3c4d5e6f",
    "method": "POST",
    "path": "/api/transactions",
    "statusCode": 500,
    "durationMs": 1205,
    "userId": "user-123",
    "exception": "InvalidOperationException: Account 'invalid' not found"
  },
  "exception": {
    "type": "System.InvalidOperationException",
    "message": "Account 'invalid' not found",
    "stackTrace": "   at BranchService.TransactionService.Process(...)..."
  }
}
```

### ✅ Observations

- ✅ LogLevel = Error (5xx)
- ✅ Exception captured
- ✅ Stack trace included
- ✅ Duration shows performance issue (1205ms)

---

## 🎯 Scenario 4: Development Mode (Body Logging)

### Configuration (Development)

```json
{
  "Logging": {
    "Global": {
      "ServiceName": "ApiGateway-Dev",
      "EnableRequestBodyLogging": true,
      "EnableResponseBodyLogging": true,
      "WhitelistedPaths": ["/api/*"]
    }
  }
}
```

### Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Debug Branch",
    "code": "DBG-001"
  }'
```

### Log Output

```json
{
  "@timestamp": "2025-01-03T10:33:00.012Z",
  "level": "Information",
  "message": "POST /api/branches responded 201 in 56ms",
  "fields": {
    "service": "ApiGateway-Dev",
    "correlationId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "method": "POST",
    "path": "/api/branches",
    "statusCode": 201,
    "durationMs": 56,
    "requestBody": "{\"name\":\"Debug Branch\",\"code\":\"DBG-001\"}",
    "responseBody": "{\"id\":123,\"name\":\"Debug Branch\",\"code\":\"DBG-001\",\"status\":\"Active\"}"
  }
}
```

### ⚠️ Observations

- ⚠️ Request body logged (dev only)
- ⚠️ Response body logged (dev only)
- ⚠️ Performance overhead higher (~70%)
- ⚠️ NEVER use in production

---

## 🎯 Scenario 5: Excluded Path (Health Check)

### Request

```bash
curl http://localhost:5000/health
```

### Log Output

```
(No log entry)
```

### ✅ Observations

- ✅ Health check excluded from logs
- ✅ No performance overhead
- ✅ Clean log storage

---

## 🎯 Scenario 6: Sensitive Headers Filtered

### Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Cookie: session=abc123; user=john" \
  -H "X-Api-Key: secret-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test"}'
```

### Log Output

```json
{
  "@timestamp": "2025-01-03T10:34:00.345Z",
  "level": "Information",
  "message": "POST /api/branches responded 201 in 45ms",
  "fields": {
    "service": "ApiGateway",
    "correlationId": "b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e",
    "method": "POST",
    "path": "/api/branches",
    "statusCode": 201,
    "durationMs": 45
  }
}
```

### ✅ Observations

- ✅ Authorization header NOT logged
- ✅ Cookie header NOT logged
- ✅ X-Api-Key header NOT logged
- ✅ Security-first design

---

## 🎯 Scenario 7: Large Body Truncated

### Configuration

```json
{
  "Logging": {
    "Global": {
      "EnableRequestBodyLogging": true,
      "MaxBodySizeToLog": 1024,
      "WhitelistedPaths": ["/api/debug/*"]
    }
  }
}
```

### Request (5KB body)

```bash
curl -X POST http://localhost:5000/api/debug/test \
  -H "Content-Type: application/json" \
  -d '{"data": "very large payload... 5KB total"}'
```

### Log Output

```json
{
  "fields": {
    "requestBody": "[Body too large: 5120 bytes]"
  }
}
```

### ✅ Observations

- ✅ Large body not logged completely
- ✅ Size indicator provided
- ✅ Prevents log explosion

---

## 🎯 Scenario 8: Client Error (400)

### Request

```bash
curl -X POST http://localhost:5000/api/branches \
  -H "Content-Type: application/json" \
  -d '{"invalid": "data"}'
```

### Log Output

```json
{
  "@timestamp": "2025-01-03T10:35:00.678Z",
  "level": "Warning",
  "message": "POST /api/branches responded 400 in 12ms",
  "fields": {
    "service": "ApiGateway",
    "correlationId": "c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f",
    "method": "POST",
    "path": "/api/branches",
    "statusCode": 400,
    "durationMs": 12
  }
}
```

### ✅ Observations

- ✅ LogLevel = Warning (4xx)
- ✅ Fast response (12ms)
- ✅ Helps identify client issues

---

## 🎯 Scenario 9: End-to-End Tracing

### Architecture

```
Mobile App
    ↓ [X-Correlation-Id: mobile-123]
API Gateway (Log 1: c-mobile-123)
    ↓ [X-Correlation-Id: mobile-123]
Branch Service (Log 2: c-mobile-123)
    ↓ [X-Correlation-Id: mobile-123]
Database (Log 3: c-mobile-123)
```

### Log 1 (Gateway)

```json
{
  "service": "ApiGateway",
  "correlationId": "mobile-123",
  "path": "/api/branches",
  "statusCode": 201,
  "durationMs": 120
}
```

### Log 2 (Branch Service)

```json
{
  "service": "BranchService",
  "correlationId": "mobile-123",
  "method": "CreateBranch",
  "durationMs": 95
}
```

### Log 3 (Database)

```json
{
  "service": "Database",
  "correlationId": "mobile-123",
  "query": "INSERT INTO Branches...",
  "durationMs": 45
}
```

### Elasticsearch Query

```json
GET /flex-*/_search
{
  "query": {
    "term": {
      "fields.correlationId.keyword": "mobile-123"
    }
  },
  "sort": [{ "@timestamp": "asc" }]
}
```

### Result

```
Total journey: 120ms
- Gateway: 120ms
  - Branch Service: 95ms
    - Database: 45ms
```

### ✅ Observations

- ✅ Full request journey visible
- ✅ Bottleneck identified (Gateway overhead: 25ms)
- ✅ Single query shows all logs

---

## 🎯 Scenario 10: Performance Analysis

### Elasticsearch Aggregation Query

```json
GET /flex-apigateway-*/_search
{
  "size": 0,
  "query": {
    "range": {
      "@timestamp": {
        "gte": "now-1h"
      }
    }
  },
  "aggs": {
    "by_path": {
      "terms": {
        "field": "fields.path.keyword"
      },
      "aggs": {
        "avg_duration": {
          "avg": {
            "field": "fields.durationMs"
          }
        },
        "p95_duration": {
          "percentiles": {
            "field": "fields.durationMs",
            "percents": [95]
          }
        }
      }
    }
  }
}
```

### Result

```json
{
  "aggregations": {
    "by_path": {
      "buckets": [
        {
          "key": "/api/branches",
          "doc_count": 1523,
          "avg_duration": { "value": 42.5 },
          "p95_duration": { "values": { "95.0": 125.0 } }
        },
        {
          "key": "/api/transactions",
          "doc_count": 8942,
          "avg_duration": { "value": 215.3 },
          "p95_duration": { "values": { "95.0": 850.0 } }
        }
      ]
    }
  }
}
```

### ✅ Observations

- ✅ `/api/branches`: Fast (avg 42ms, p95 125ms)
- ⚠️ `/api/transactions`: Slow (avg 215ms, p95 850ms)
- 🔴 Action: Optimize transactions endpoint

---

## 🎯 Scenario 11: Security Monitoring

### Query: Failed Authentication Attempts

```json
GET /flex-apigateway-*/_search
{
  "query": {
    "bool": {
      "must": [
        { "term": { "fields.statusCode": 401 } },
        { "range": { "@timestamp": { "gte": "now-1h" } } }
      ]
    }
  },
  "aggs": {
    "by_ip": {
      "terms": {
        "field": "fields.ipAddress.keyword",
        "size": 10
      }
    }
  }
}
```

### Result

```json
{
  "aggregations": {
    "by_ip": {
      "buckets": [
        { "key": "192.168.1.50", "doc_count": 127 },
        { "key": "10.0.0.15", "doc_count": 8 }
      ]
    }
  }
}
```

### ✅ Observations

- 🔴 IP `192.168.1.50`: 127 failed attempts (suspicious!)
- ⚠️ Consider rate limiting or IP blocking

---

## 📊 Summary Table

| Scenario | Body Logged | Headers Logged | Performance | Use Case |
|----------|-------------|----------------|-------------|----------|
| Normal Request | ❌ | ✅ (filtered) | Fast | Production |
| With CorrelationId | ❌ | ✅ | Fast | Production |
| Error (500) | ❌ | ✅ | Slow | Debug |
| Dev Mode | ✅ | ✅ | Slow | Development |
| Health Check | ❌ | ❌ | No overhead | Production |
| Sensitive Headers | ❌ | ❌ (filtered) | Fast | Production |
| Large Body | ⚠️ (truncated) | ✅ | Medium | Development |
| Client Error | ❌ | ✅ | Fast | Production |
| End-to-End | ❌ | ✅ | Fast | Tracing |
| Performance Analysis | ❌ | ✅ | Fast | Monitoring |
| Security Monitoring | ❌ | ✅ | Fast | Security |

---

## 🔗 Related

- [Configuration Guide](./GlobalLogging-Configuration.md)
- [ADR - Global Logging](./ADR-Global-Logging.md)
- [IT Audit Checklist](./IT-Audit-Checklist.md)

