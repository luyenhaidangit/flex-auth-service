namespace Flex.Infrastructures.Responses
{
    public class ErrorInfo
    {
        public int StatusCode { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string? Message { get; set; }
        public object? Errors { get; set; }
    }
}
