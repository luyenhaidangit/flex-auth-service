# Changelog

## Unreleased

### Changed

- Centralize Serilog logging configuration resolution and emit ECS `service.name` as `auth-service` by default, with `Logging:ServiceName` available for overrides.
- Align log field names with ECS by writing request IDs to `http.request.id` and response body content to `labels.http_response_body`.
- Write Serilog properties as root OpenSearch fields so ECS names such as `log.level`, `service.name`, and `trace.id` are populated directly.
- Normalize `service.environment` log values to lowercase for stable Elasticsearch aggregations.
- Remove the in-process Logstash connection monitor so Logstash availability can be handled by external monitoring.
- Remove unused Logstash health-check settings from configuration after dropping the connection monitor.
- Rename the ECS Serilog enricher to better describe its field-enrichment role.
- Make the ECS Serilog field enricher public for reuse by other application assemblies.

### Fixed

- Only report the Logstash sink endpoint as invalid when the configured URI cannot be parsed.

### Added

- Add project-local Codex skill lock for `code-changelog` and ignore synced skill artifacts.
