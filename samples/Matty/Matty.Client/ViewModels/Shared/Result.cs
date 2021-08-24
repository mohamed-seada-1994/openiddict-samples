using System.Collections.Immutable;

namespace Matty.Client.ViewModels.Shared
{
    public class Result<T>
    {
        public bool HasErrors { get; protected set; }
        public bool Succeed => !HasErrors;
        public IImmutableList<string> Errors { get; protected set; }
        public T Data { get; protected set; }

        protected Result()
        { }

        public static Result<T> Success(T data)
            => new Result<T> { 
                HasErrors = false,
                Data = data
            };

        public static Result<T> Failure(params string[] errors)
            => new Result<T>
            {
                HasErrors = true,
                Errors = errors.ToImmutableList()
            };
    }
}
