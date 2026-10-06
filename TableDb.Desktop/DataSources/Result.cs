namespace TableDb.Desktop.DataSources;

public class Result<T>
{
    public bool IsOk { get; }
    public string Error { get; }
    public T Value { get; }

    private Result(bool isOk, string error, T value)
    {
        IsOk = isOk;
        Error = error;
        Value = value;
    }

    public static Result<T> Ok(T value) => new Result<T>(true, string.Empty, value);

    public static Result<T> Fail(string error) => new Result<T>(false, error, default!);
}
