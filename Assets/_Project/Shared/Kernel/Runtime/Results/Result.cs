namespace CleanTime.Shared.Kernel
{
    /// <summary>Outcome of an operation whose failure is an expected business outcome.</summary>
    public readonly struct Result
    {
        private Result(bool success, Error? error)
        {
            IsSuccess = success;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public Error? Error { get; }

        public string ErrorCode => Error.HasValue ? Error.Value.Code : string.Empty;

        public static Result Success() => new Result(true, null);

        public static Result Failure(string code, string message) => new Result(false, new Error(code, message));

        public static Result Failure(Error error) => new Result(false, error);

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);

        public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(code, message);

        public override string ToString() => IsSuccess ? "Success" : $"Failure({Error})";
    }

    public readonly struct Result<T>
    {
        private Result(bool isSuccess, T value, Error? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public T Value { get; }

        public Error? Error { get; }

        public string ErrorCode => Error.HasValue ? Error.Value.Code : string.Empty;

        public static Result<T> Success(T value) => new Result<T>(true, value, null);

        public static Result<T> Failure(string code, string message) =>
            new Result<T>(false, default, new Error(code, message));

        public static Result<T> Failure(Error error) => new Result<T>(false, default, error);

        /// <summary>Carries a failed untyped result across a typed boundary.</summary>
        public static Result<T> FromFailure(Result failed) =>
            failed.Error.HasValue ? Failure(failed.Error.Value) : Failure("result.unknown", "Unknown failure.");

        public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Error.Value);

        public static implicit operator Result(Result<T> result) => result.ToResult();

        public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure({Error})";
    }
}
