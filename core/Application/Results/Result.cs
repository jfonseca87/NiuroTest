namespace Niuro.Core.Application.Results;

/// <summary>
/// Result pattern para representar éxito o falla sin valor.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("No se puede tener error en resultado exitoso.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Un resultado fallido debe tener un error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(isSuccess: true, error: null);
    public static Result Failure(string error) => new(isSuccess: false, error: error);
    public static Result<T> Success<T>(T value) => new(value, isSuccess: true, error: null);
    public static Result<T> Failure<T>(string error) => new(default!, isSuccess: false, error: error);
}
