using System;

namespace Common.Results
{
    public struct ErrorResult : IResult
    {
        public bool IsSuccess { get; }
        public object Result { get; }

        public readonly string Message;
        public readonly Exception Exception;

        public ErrorResult(string message, Exception exception = null)
        {
            IsSuccess = false;
            Result = null;
            Message = message;
            Exception = exception;
        }

        public ErrorResult(Exception exception) : this(exception.Message, exception)
        { }
    }
}