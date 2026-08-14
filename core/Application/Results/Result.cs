namespace Niuro.Core.Application.Results;

/// <summary>
/// Result pattern to represent success or failure without a value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot contain an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(isSuccess: true, error: null);
    public static Result Failure(string error) => new(isSuccess: false, error: error);
    public static Result<T> Success<T>(T value) => new(value, isSuccess: true, error: null);
    public static Result<T> Failure<T>(string error) => new(default!, isSuccess: false, error: error);
}
