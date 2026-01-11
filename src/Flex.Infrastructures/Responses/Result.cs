
using System.Text.Json.Serialization;

namespace Flex.Infrastructures.Responses
{
    public class Result
    {
        [JsonPropertyOrder(1)]
        public bool IsSuccess { get; set; }

        [JsonPropertyOrder(2)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        [JsonPropertyOrder(3)]
        public string? Message { get; set; }

        [JsonPropertyOrder(4)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }

        [JsonPropertyOrder(5)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; set; }

        public Result(bool isSuccess, string? message, object? data = default, object? errors = default, string? errorCode = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
            Errors = errors;
            ErrorCode = errorCode;
        }

        public static Result Success(object? data = default, string? message = null, string? errorCode = null)
        {
            return new Result(true, message, data, errorCode: errorCode);
        }

        public static Result Failure(object? errors = default, string? message = null, string? errorCode = null)
        {
            return new Result(false, message, default, errors, errorCode);
        }
    }
}
