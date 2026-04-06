namespace BigDaddyProject.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    protected Result(bool success, string? error, int code)
    {
        IsSuccess = success; Error = error; StatusCode = code;
    }

    public static Result Success() => new(true, null, 200);
    public static Result Failure(string error, int code = 400) => new(false, error, code);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool success, T? value, string? error, int code)
        : base(success, error, code) { Value = value; }

    public static Result<T> Success(T value) => new(true, value, null, 200);
    public new static Result<T> Failure(string error, int code = 400) => new(false, default, error, code);
}