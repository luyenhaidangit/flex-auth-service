# Flex Auth Service - Cursor Rules Index

Tài liệu này cung cấp tổng quan về các rule được sử dụng trong dự án Flex Auth Service để hướng dẫn AI assistant trong quá trình phát triển.

## 📋 Danh sách Rules

### 1. [Technical Design Documentation Rule](./technical_design_documentation_rule.md)
**Mục đích:** Hướng dẫn tạo Technical Design Document (TDD) từ feature request hoặc user story.

**Khi nào sử dụng:**
- Khi cần thiết kế kỹ thuật cho một feature mới
- Khi cần phân tích và đề xuất giải pháp cho một yêu cầu

**Nội dung chính:**
- Workflow: Hiểu request → Phân tích codebase → Tạo TDD
- Cấu trúc TDD: Overview, Requirements, Technical Design, Testing Plan, Open Questions
- Sử dụng Mermaid diagrams cho sequence diagrams và ERD
- Tuân thủ coding standards và conventions của project

**Output:** Technical Design Document (.md)

---

### 2. [Task Breakdown Rule](./break_down_rule.md)
**Mục đích:** Chia nhỏ Technical Design Document thành các task cụ thể, có thể thực hiện được.

**Khi nào sử dụng:**
- Sau khi có Technical Design Document
- Cần tạo checklist các task để implement

**Nội dung chính:**
- Guidelines: Granularity, Actionable, Dependencies, Completeness
- Format: Markdown checklist với `- [ ]` syntax
- Categorization: Database, API, UI, Testing, Documentation
- Prioritization: Đánh dấu task có priority cao

**Output:** Task Breakdown Checklist (.md)

---

### 3. [Implementation Rule](./implementation_rule.md)
**Mục đích:** Hướng dẫn implement các task từ checklist theo Technical Design Document.

**Khi nào sử dụng:**
- Khi bắt đầu implement một task cụ thể
- Khi code theo TDD và task breakdown

**Nội dung chính:**
- Workflow: Receive Task → Review TDD → Implement → Update Checklist → Commit
- Coding Standards: C# conventions, Clean Architecture, CQRS, Domain-Driven Design
- General Principles: SOLID, DRY, YAGNI
- Checklist Discipline: Luôn update checklist sau khi hoàn thành task

**Output:** Code implementation + Updated checklist

---

### 4. [Project Overview Example](./project_overview_example.md)
**Mục đích:** Ví dụ về cấu trúc project và các pattern được sử dụng (tham khảo từ BoneNet project).

**Khi nào sử dụng:**
- Tham khảo khi cần hiểu về project structure
- Tham khảo về các pattern và conventions

**Lưu ý:** File này là example từ project khác (BoneNet), chỉ dùng để tham khảo. Cấu trúc thực tế của Flex Auth Service có thể khác.

---

## 🔄 Workflow Tổng Quan

```
Feature Request
    ↓
[Technical Design Documentation Rule]
    ↓
Technical Design Document (TDD)
    ↓
[Task Breakdown Rule]
    ↓
Task Breakdown Checklist
    ↓
[Implementation Rule] (lặp lại cho từng task)
    ↓
Implementation + Tests
    ↓
Updated Checklist
    ↓
Commit
```

## 📝 Lưu ý Quan Trọng

1. **Thứ tự thực hiện:**
   - Luôn bắt đầu với Technical Design Document
   - Sau đó mới tạo Task Breakdown
   - Cuối cùng mới implement từng task

2. **Consistency:**
   - Tất cả các rule đều tuân thủ Clean Architecture
   - Sử dụng Domain-Driven Design principles
   - Follow C# coding conventions

3. **Documentation:**
   - Luôn cập nhật documentation khi implement
   - Sử dụng XML documentation comments cho code
   - Update checklist ngay sau khi hoàn thành task

4. **Quality:**
   - Viết unit tests cho tất cả functionality mới
   - Code phải reflect chính xác TDD
   - Nếu có discrepancy, dừng lại và clarify

## 🎯 Best Practices

- **Đọc kỹ rule trước khi bắt đầu:** Mỗi rule có workflow và guidelines riêng
- **Hỏi khi không rõ:** Đừng đoán, luôn hỏi để clarify
- **Update checklist ngay:** Không để checklist outdated
- **Follow conventions:** Tuân thủ coding standards đã định nghĩa
- **Test trước khi mark done:** Chỉ mark task done khi đã test và verify

## 📚 Thêm Rule Mới

Khi thêm rule mới:
1. Tạo file `.md` trong thư mục `rules/`
2. Cập nhật file `README.md` này với thông tin về rule mới
3. Đảm bảo rule mới consistent với các rule hiện có

---

**Last Updated:** 2025-01-15
