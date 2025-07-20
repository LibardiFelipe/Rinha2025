using System.Diagnostics.CodeAnalysis;

namespace MinimalArchitecture.Template.Domain.Utils
{
    public sealed class Result<TContent>
    {
        private Result(bool isSuccess, TContent? content)
        {
            IsSuccess = isSuccess;
            Content = content;
        }

        [MemberNotNullWhen(true, nameof(Content))]
        public bool IsSuccess { get; init; }
        public TContent? Content { get; init; }

        public static Result<TContent> Success(TContent content) =>
            new(isSuccess: true, content);

        public static Result<TContent> Failure(TContent? content) =>
            new(isSuccess: false, content);
    }
}
