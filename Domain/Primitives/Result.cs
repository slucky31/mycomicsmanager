/*
 * Source :
 * - https://www.youtube.com/watch?v=uOEDM0c9BNI&t=2s
 * - https://www.youtube.com/watch?v=WCCkEe_Hy2Y&list=RDCMUCC_dVe-RI-vgCZfls06mDZQ&index=2
 */


namespace Domain.Primitives;

public readonly struct Result<TValue>
{

    private Result(TValue value)
    {        
        Value = value;
        IsSuccess = true;
        Error = default;
    }
    private Result(TError error)
    {
        Value = default;
        IsSuccess = true;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private readonly TError? Error { get; }

    private readonly TValue? Value { get; }

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(TError error) => new(error);

    public static implicit operator Result<TValue>(TError error) => Result<TValue>.Failure(error);
}
