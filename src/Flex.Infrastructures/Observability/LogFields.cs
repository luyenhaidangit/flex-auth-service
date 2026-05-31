namespace Flex.Infrastructures.Observability;

public static class LogFields
{
    public const string Timestamp = "@timestamp";
    public const string Message = "message";
    public const string EcsVersion = "ecs.version";

    public const string LogLevel = "log.level";
    public const string LogLogger = "log.logger";

    public const string ServiceName = "service.name";
    public const string ServiceVersion = "service.version";
    public const string ServiceEnvironment = "service.environment";

    public const string HostName = "host.name";
    public const string ProcessThreadName = "process.thread.name";

    public const string TraceId = "trace.id";
    public const string TransactionId = "transaction.id";
    public const string SpanId = "span.id";

    public const string UserId = "user.id";
    public const string UserName = "user.name";

    public const string EventAction = "event.action";
    public const string EventCategory = "event.category";
    public const string EventType = "event.type";
    public const string EventOutcome = "event.outcome";
    public const string EventDuration = "event.duration";
    public const string EventDurationMs = "event.duration_ms";

    public const string HttpRequestMethod = "http.request.method";
    public const string HttpRequestBodyContent = "http.request.body.content";
    public const string HttpResponseStatusCode = "http.response.status_code";
    public const string HttpResponseBodyContent = "labels.http_response_body";

    public const string UrlPath = "url.path";
    public const string UrlFull = "url.full";
    public const string UrlQuery = "url.query";

    public const string ClientIp = "client.ip";
    public const string ServerIp = "server.ip";

    public const string UserAgentOriginal = "user_agent.original";

    public const string ErrorType = "error.type";
    public const string ErrorMessage = "error.message";
    public const string ErrorStackTrace = "error.stack_trace";

    public const string CorrelationId = "labels.correlation_id";
    public const string RequestId = "http.request.id";
    public const string ClientId = "labels.client_id";
    public const string TenantId = "labels.tenant_id";
    public const string Domain = "labels.domain";
    public const string Module = "labels.module";
    public const string Function = "labels.function";
}
