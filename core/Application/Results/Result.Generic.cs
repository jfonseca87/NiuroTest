namespace Niuro.Core.Application.Results;

/// <summary>
/// Result pattern para representar éxito o falla con valor.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede obtener valor de resultado fallido.");

    public new string? Error => base.Error;

    public static implicit operator Result<T>(T value) => Success(value);
}
