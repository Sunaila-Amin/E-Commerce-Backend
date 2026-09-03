namespace ECommerce.Business.Contracts;

public sealed class ServiceResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ServiceResult Success(string? message = null) =>
        new() { Succeeded = true, Message = message };

    public static ServiceResult Failure(string message, IReadOnlyList<string>? errors = null) =>
        new() { Succeeded = false, Message = message, Errors = errors };

    public static ServiceResult Failure(IReadOnlyList<string> errors) =>
        new() { Succeeded = false, Message = "One or more validation errors occurred.", Errors = errors };
}

public sealed class ServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ServiceResult<T> Success(T data, string? message = null) =>
        new() { Succeeded = true, Data = data, Message = message };

    public static ServiceResult<T> Failure(string message, IReadOnlyList<string>? errors = null) =>
        new() { Succeeded = false, Message = message, Errors = errors };

    public static ServiceResult<T> Failure(IReadOnlyList<string> errors) =>
        new() { Succeeded = false, Message = "One or more validation errors occurred.", Errors = errors };
}
