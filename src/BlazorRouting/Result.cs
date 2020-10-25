namespace BlazorRouting
{
    public class Result<T>
    {
        public Result(T value, bool isSuccess, string? message)
        {
            Value = value;
            IsSuccess = isSuccess;
            Message = message;
        }

        public T Value { get; }
        public bool IsSuccess { get; }
        public string? Message { get; }

        public static Result<T> Success(T value) => new Result<T>(value, true, null);
        public static Result<T> Failed(T value = default, string? message = null) => new Result<T>(value!, false, message);
    }
}
