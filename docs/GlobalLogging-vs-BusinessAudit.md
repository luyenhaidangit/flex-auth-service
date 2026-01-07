# Global Logging vs Business Audit - Separation Guide

## 📋 Tổng quan

Trong hệ thống enterprise/banking, **QUAN TRỌNG** phải tách biệt:

1. **Global Logging** (Technical logs)
2. **Business Audit** (Business events)

Việc nhầm lẫn 2 loại này là **lỗi kiến trúc nghiêm trọng**.

---

## 🔍 So sánh chi tiết

| Tiêu chí               | Global Logging                      | Business Audit                        |
|------------------------|-------------------------------------|---------------------------------------|
| **Mục đích**           | Quan sát kỹ thuật hệ thống          | Ghi nhận hành vi nghiệp vụ            |
| **Câu hỏi trả lời**    | HOW? (System behavior)              | WHAT? (Business action)               |
| **Người dùng**         | Developer, DevOps, SRE              | Auditor, Business Analyst, Compliance |
| **Lưu ở đâu**          | Elasticsearch/OpenSearch            | **Audit Database (relational)**       |
| **Retention**          | 7-30 ngày                           | **5-10 năm**                          |
| **Format**             | JSON (unstructured)                 | **Structured table**                  |
| **Khi nào ghi**        | Mọi request/response                | **Chỉ business events**               |
| **Có bị xóa không?**   | Có (TTL)                            | **KHÔNG BAO GIỜ**                     |
| **Compliance**         | KHÔNG                               | **CÓ** (SOX, GDPR, Basel)             |

---

## ✅ Global Logging - Technical Observability

### Vai trò
> Giám sát **hành vi kỹ thuật** của hệ thống

### Log GÌ?

```json
{
  "service": "ApiGateway",
  "correlationId": "c-123",
  "method": "POST",
  "path": "/api/branches",
  "statusCode": 201,
  "durationMs": 42,
  "userId": "user-123",
  "timestamp": "2025-01-03T10:30:00Z"
}
```

### Câu hỏi trả lời

- ✅ Request này mất bao lâu?
- ✅ Service nào bị lỗi?
- ✅ User nào gây ra nhiều 500 error?
- ✅ API nào chậm nhất?

### Lưu ở đâu

```
Elasticsearch / OpenSearch
└── Index: flex-apigateway-2025-01
    ├── TTL: 30 days
    └── Auto-delete after retention
```

### Khi nào XÓA?

✅ **CÓ THỂ XÓA** sau 30 ngày

---

## 📊 Business Audit - Compliance Logging

### Vai trò
> Ghi nhận **sự kiện nghiệp vụ quan trọng**

### Log GÌ?

```sql
-- Audit Table
CREATE TABLE AuditLogs (
    Id              BIGINT PRIMARY KEY,
    Timestamp       DATETIME NOT NULL,
    UserId          VARCHAR(50) NOT NULL,
    Action          VARCHAR(100) NOT NULL,     -- CreateBranch, UpdateAccount, TransferMoney
    EntityType      VARCHAR(50) NOT NULL,      -- Branch, Account, Transaction
    EntityId        VARCHAR(100),              -- branch-123, acc-456
    OldValue        NVARCHAR(MAX),             -- JSON of old state
    NewValue        NVARCHAR(MAX),             -- JSON of new state
    IpAddress       VARCHAR(50),
    CorrelationId   VARCHAR(100),
    Reason          NVARCHAR(500)              -- User's note if applicable
)
```

### Ví dụ Audit Entry

```json
{
  "id": 123456,
  "timestamp": "2025-01-03T10:30:00Z",
  "userId": "user-123",
  "action": "CreateBranch",
  "entityType": "Branch",
  "entityId": "branch-789",
  "oldValue": null,
  "newValue": {
    "name": "Ha Noi Branch",
    "code": "HN-001",
    "status": "Active"
  },
  "ipAddress": "192.168.1.100",
  "correlationId": "c-123"
}
```

### Câu hỏi trả lời

- ✅ Ai tạo branch này?
- ✅ Khi nào account này bị khóa?
- ✅ Giá trị cũ của transaction là gì?
- ✅ User này đã làm gì trong 6 tháng qua?

### Lưu ở đâu

```
SQL Server / PostgreSQL (Audit Database)
└── Table: AuditLogs
    ├── Retention: 5-10 năm
    ├── Encrypted: YES
    ├── Backup: Daily
    └── KHÔNG BAO GIỜ XÓA (except compliance rules)
```

### Khi nào XÓA?

❌ **KHÔNG XÓA** (hoặc theo compliance: 7-10 năm)

---

## 🚫 Common Mistakes

### ❌ MISTAKE 1: Log nghiệp vụ vào ELK

```csharp
// ❌ WRONG
_logger.LogInformation("User {UserId} created branch {BranchName}", 
    userId, branchName);

// ✅ RIGHT
await _auditService.LogAsync(new AuditEntry
{
    Action = "CreateBranch",
    EntityType = "Branch",
    EntityId = branchId,
    NewValue = JsonSerializer.Serialize(branch)
});
```

### ❌ MISTAKE 2: Log kỹ thuật vào Audit DB

```csharp
// ❌ WRONG
await _auditService.LogAsync(new AuditEntry
{
    Action = "HttpRequest",  // ← This is technical, not business
    NewValue = "POST /api/branches 201 42ms"
});

// ✅ RIGHT
// Use GlobalLoggingMiddleware - auto log to ELK
```

### ❌ MISTAKE 3: Dùng ELK làm audit storage

```
❌ BAD Architecture:
Business Event → ELK → TTL 30 days → DELETED
  ↓
Auditor cần report 1 năm trước → KHÔNG TÌM THẤY ❌

✅ GOOD Architecture:
Business Event → Audit DB → 10 năm → Có thể query ✅
```

---

## 🏗️ Correct Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   API Gateway                            │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ GlobalLoggingMiddleware                         │    │
│  │   → Log technical metadata                     │    │
│  └────────────────────────────────────────────────┘    │
│                          ↓                              │
│                  Elasticsearch/ELK                      │
│                  (30 days retention)                    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   Service Layer                          │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ Business Logic                                  │    │
│  │   → Call AuditService.LogAsync()               │    │
│  └────────────────────────────────────────────────┘    │
│                          ↓                              │
│              Audit Database (SQL)                       │
│              (5-10 years retention)                     │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 Decision Matrix

### Khi nào dùng Global Logging?

| Scenario                          | Global Logging | Business Audit |
|-----------------------------------|----------------|----------------|
| Monitor API performance           | ✅              | ❌              |
| Debug 500 errors                  | ✅              | ❌              |
| Trace request journey             | ✅              | ❌              |
| Security monitoring (AuthN/AuthZ) | ✅              | ⚠️ (Both)      |
| Rate limiting logs                | ✅              | ❌              |

### Khi nào dùng Business Audit?

| Scenario                          | Global Logging | Business Audit |
|-----------------------------------|----------------|----------------|
| User created account              | ❌              | ✅              |
| Branch status changed             | ❌              | ✅              |
| Transaction approved              | ❌              | ✅              |
| Permission modified               | ⚠️ (Both)      | ✅              |
| Data exported                     | ❌              | ✅              |

---

## 🎯 Implementation Guidelines

### In Controller/Service

```csharp
public class BranchService
{
    private readonly ILogger<BranchService> _logger;        // ← Technical logging
    private readonly IAuditService _auditService;           // ← Business audit
    
    public async Task<Branch> CreateAsync(CreateBranchDto dto)
    {
        // Technical log - auto via GlobalLoggingMiddleware
        // No need to log manually here
        
        var branch = new Branch { /* ... */ };
        await _repository.AddAsync(branch);
        
        // ✅ Business audit - MUST be explicit
        await _auditService.LogAsync(new AuditEntry
        {
            Action = "CreateBranch",
            EntityType = nameof(Branch),
            EntityId = branch.Id,
            NewValue = JsonSerializer.Serialize(branch),
            UserId = _currentUser.Id,
            CorrelationId = _httpContext.TraceIdentifier
        });
        
        return branch;
    }
}
```

---

## 🔒 Security & Compliance

### Global Logging

```json
// ✅ Log này để debug, có thể xóa
{
  "correlationId": "c-123",
  "method": "POST",
  "path": "/api/branches",
  "statusCode": 201,
  "durationMs": 42
}
```

### Business Audit

```json
// ❗ Log này để audit, KHÔNG được xóa
{
  "action": "CreateBranch",
  "userId": "user-123",
  "entityId": "branch-789",
  "newValue": {
    "name": "Ha Noi Branch",
    "code": "HN-001"
  },
  "timestamp": "2025-01-03T10:30:00Z"
}
```

---

## 📊 Storage Comparison

### Global Logging Storage (ELK)

```
Pros:
✅ Fast full-text search
✅ Great for aggregation
✅ Time-series optimization
✅ Horizontal scaling

Cons:
❌ No ACID transactions
❌ Data can be lost
❌ Not for long-term storage
❌ Expensive for large retention
```

### Business Audit Storage (SQL)

```
Pros:
✅ ACID transactions
✅ Data integrity
✅ Long-term storage
✅ Cheap for archival
✅ Compliance-ready

Cons:
❌ Slower for analytics
❌ Limited full-text search
❌ Harder to scale horizontally
```

---

## ✅ Checklist

### Global Logging Setup

- [x] GlobalLoggingMiddleware implemented
- [x] CorrelationId propagation
- [x] Sensitive data filtering
- [x] Elasticsearch integration
- [x] TTL configured (30 days)

### Business Audit Setup

- [ ] Audit database schema
- [ ] AuditService implementation
- [ ] Integration in business logic
- [ ] Long-term retention (5-10 years)
- [ ] Backup strategy
- [ ] Encryption at rest
- [ ] Access control (who can query?)

---

## 🆘 FAQ

### Q: Có thể log cả 2 không?

**A**: Có, nhưng CHỈ khi:
- Sự kiện VỪA là technical VỪA là business
- VD: AuthN failure (technical error + security audit)

### Q: ELK đủ rẻ để lưu audit?

**A**: KHÔNG. ELK:
- $$ Đắt cho long-term storage
- ❌ Không đảm bảo data integrity
- ❌ Không đáp ứng compliance

### Q: Có thể dùng MongoDB cho audit?

**A**: ⚠️ Cân nhắc:
- ✅ ACID transactions (từ v4.0+)
- ⚠️ Cần configure đúng
- ✅ Dễ scale
- ❌ Ít tool audit sẵn

**Khuyến nghị**: SQL (PostgreSQL/SQL Server)

---

## 📚 Related Documents

- [Global Logging Configuration](./GlobalLogging-Configuration.md)
- [ADR - Global Logging](./ADR-Global-Logging.md)
- [Business Audit Implementation Guide](./Business-Audit-Guide.md)

---

## 📞 Summary

| Aspect              | Global Logging         | Business Audit          |
|---------------------|------------------------|-------------------------|
| **Purpose**         | System observability   | Business compliance     |
| **Storage**         | Elasticsearch (ELK)    | **SQL Database**        |
| **Retention**       | 7-30 days              | **5-10 years**          |
| **Can delete?**     | ✅ Yes                  | **❌ No**               |
| **When to log?**    | Every request          | **Business events only**|
| **Implementation**  | Middleware (auto)      | **Manual in code**      |

**Golden Rule**: 
> If you need to answer "WHO did WHAT to WHICH entity", it's **Business Audit**.
> If you need to answer "HOW FAST or WHY ERROR", it's **Global Logging**.

