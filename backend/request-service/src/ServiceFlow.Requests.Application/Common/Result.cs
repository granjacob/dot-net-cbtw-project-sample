namespace ServiceFlow.Requests.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error Validation(string code, string message, string field, params string[] errors) =>
        new(code, message, ErrorType.Validation, new Dictionary<string, string[]> { [field] = errors });

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

public sealed record Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}
