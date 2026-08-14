namespace Niuro.Core.Application.Results;

/// <summary>
/// Result pattern to represent success or failure with a value.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public new string? Error => base.Error;

    public static implicit operator Result<T>(T value) => Success(value);
}
