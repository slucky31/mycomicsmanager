/*
 * Source :
 * - https://www.youtube.com/watch?v=uOEDM0c9BNI&t=2s
 * - https://www.youtube.com/watch?v=WCCkEe_Hy2Y&list=RDCMUCC_dVe-RI-vgCZfls06mDZQ&index=2
 * - https://github.com/altmann/FluentResults/blob/master/src/FluentResults/Results/ResultBase.cs
 */

namespace Domain.Primitives;

public sealed class Result : ResultBase
{

    private Result()
    {
        IsSuccess = true;
        Error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        Error = error;
    }

    // The factories of Result<TValue> live here too: a static member of a generic type repeats its type argument (CA1000).
    public static Result Success()
    {
        return new();
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new(value);
    }

    public static Result Failure(TError error)
    {
        return new(error);
    }

    public static Result<TValue> Failure<TValue>(TError error)
    {
        return new(error);
    }

    public static implicit operator Result(TError error)
    {
        return Result.Failure(error);
    }

    public static Result<TError> ToResult(TError error)
    {
        return Failure<TError>(error);
    }
}

public sealed class Result<TValue> : ResultBase, IResult<TValue>
{

    public TValue? Value { get; init; }

    internal Result(TValue value)
    {
        Value = value;
        IsSuccess = true;
        Error = default;
    }

    internal Result(TError error)
    {
        Value = default;
        IsSuccess = false;
        Error = error;
    }

    public static implicit operator Result<TValue>(TError error)
    {
        return new(error);
    }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new(value);
    }
}
