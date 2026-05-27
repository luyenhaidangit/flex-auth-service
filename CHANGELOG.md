# Changelog

## Unreleased

### Changed

- Align log field names with ECS by writing request IDs to `http.request.id` and response body content to `labels.http_response_body`.
- Write Serilog properties as root OpenSearch fields so ECS names such as `log.level`, `service.name`, and `trace.id` are populated directly.

### Added

- Add project-local Codex skill lock for `code-changelog` and ignore synced skill artifacts.
