/*
 * Source :
 * - https://www.youtube.com/watch?v=uOEDM0c9BNI&t=2s
 * - https://www.youtube.com/watch?v=WCCkEe_Hy2Y&list=RDCMUCC_dVe-RI-vgCZfls06mDZQ&index=2
 * - https://github.com/altmann/FluentResults/blob/master/src/FluentResults/Results/ResultBase.cs
 */

namespace Domain.Primitives;

public interface IResultBase
{
    bool IsSuccess { get; init; }

    bool IsFailure { get;  }
    
}

public interface IResult<out TValue> : IResultBase
{
    TValue? Value { get; }
}

public abstract class ResultBase : IResultBase
{
    public bool IsSuccess { get; init; }

    public bool IsFailure => !IsSuccess;

    public TError? Error { get; init;  }
    
}

public abstract class ResultBase<TResult> : ResultBase where TResult : ResultBase<TResult>;
