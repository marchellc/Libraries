using System;

namespace Common.Results
{
    public struct Error : IResult
    {
        public bool IsSuccess { get; }
        public object Result { get; }

        public readonly string Message;
        public readonly Exception Exception;

        public Error(string message, Exception exception = null)
        {
            IsSuccess = false;
            Result = null;

            Message = message;
            Exception = exception;
        }

        public Error(Exception exception) : this(exception.Message, exception) { }
    }
}