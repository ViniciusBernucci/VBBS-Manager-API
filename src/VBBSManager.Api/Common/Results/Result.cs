namespace VBBSManager.Api.Common.Results;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    private Result(bool success, string? error) { IsSuccess = success; Error = error; }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}
