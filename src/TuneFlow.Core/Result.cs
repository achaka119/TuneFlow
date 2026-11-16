namespace TuneFlow.Core;

// Handwritten success/failure type, using the same idea as the assignments.
// It needs no functional-programming package.
public sealed class Result<T>
{
    private readonly T? value;

    public bool IsSuccess { get; }
    public bool IsFailure { get { return !IsSuccess; } }
    public string? Error { get; }

    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("A failed result has no value.");
            return value!;
        }
    }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        this.value = value;
        Error = error;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("An error message is required.", nameof(error));

        return new Result<T>(false, default, error);
    }

    public Result<TNext> Map<TNext>(Func<T, TNext> transform)
    {
        if (IsFailure)
            return Result<TNext>.Failure(Error!);

        return Result<TNext>.Success(transform(Value));
    }

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next)
    {
        if (IsFailure)
            return Result<TNext>.Failure(Error!);

        return next(Value);
    }
}
