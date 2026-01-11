namespace Flex.Identity.Errors
{
    public class ErrorCodes
    {
        // Client error codes (4xx)
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotFound = "NOT_FOUND";
        public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
        public const string TooManyRequests = "TOO_MANY_REQUESTS";
        public const string Conflict = "CONFLICT";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string InvalidToken = "INVALID_TOKEN";
        public const string TokenExpired = "TOKEN_EXPIRED";
        public const string InvalidRequestId = "INVALID_REQUEST_ID";
        public const string RequestNotFound = "REQUEST_NOT_FOUND";
        public const string BranchNotFound = "BRANCH_NOT_FOUND";
        public const string RoleNotFound = "ROLE_NOT_FOUND";
        public const string TemplateNotFound = "TEMPLATE_NOT_FOUND";
        public const string SequenceNotFound = "SEQUENCE_NOT_FOUND";

        // Server error codes (5xx)
        public const string BadGateway = "BAD_GATEWAY";
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
        public const string DatabaseError = "DATABASE_ERROR";
        public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
        public const string ProcessingError = "PROCESSING_ERROR";
        public const string IntegrationError = "INTEGRATION_ERROR";
        public const string TimeoutError = "TIMEOUT_ERROR";

        // Identity
        public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
    }
}
