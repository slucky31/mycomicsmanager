﻿/*
 * Source :
 * - https://www.youtube.com/watch?v=uOEDM0c9BNI&t=2s
 * - https://www.youtube.com/watch?v=WCCkEe_Hy2Y&list=RDCMUCC_dVe-RI-vgCZfls06mDZQ&index=2
 * - https://github.com/altmann/FluentResults/blob/master/src/FluentResults/Results/ResultBase.cs
 */

namespace Domain.Primitives;

public class Result : ResultBase<Result>
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

    public static Result Success()
    {
        return new();
    }

    public static Result Failure(TError error)
    {
        return new(error);
    }

    public static implicit operator Result(TError error)
    {
        return Result.Failure(error);
    }

    public static Result<TError> ToResult(TError error)
    {
        return Result<TError>.Failure(error);
    }
}

public class Result<TValue> : ResultBase<Result<TValue>>, IResult<TValue>
{

    public TValue? Value { get; init; }

    private Result(TValue value)
    {
        Value = value;
        IsSuccess = true;
        Error = default;
    }

    private Result(TError error)
    {
        Value = default;
        IsSuccess = false;
        Error = error;
    }

    public static Result<TValue> Success(TValue value)
    {
        return new(value);
    }

    public static Result<TValue> Failure(TError error)
    {
        return new(error);
    }

    public static implicit operator Result<TValue>(TError error)
    {
        return Result<TValue>.Failure(error);
    }

    public static implicit operator Result<TValue>(TValue value)
    {
        return Result<TValue>.Success(value);
    }

    public static Result<TValue> ToResult(TValue value)
    {
        return Result<TValue>.Success(value);
    }

    public static Result<TValue> ToResult(TError error)
    {
        return Result<TValue>.Failure(error);
    }
}
