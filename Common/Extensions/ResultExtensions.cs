using Common.Pooling.Pools;
using Common.Results;

using System;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class ResultExtensions
    {
        public static readonly IResult[] EmptyResults = [];

        public static IResult Error()
            => new ErrorResult(null, null);

        public static IResult Error(string message)
            => new ErrorResult(message, null);

        public static IResult Error(Exception exception)
            => new ErrorResult(exception);

        public static IResult Error(string message, Exception exception)
            => new ErrorResult(message, exception);

        public static IResult Success(object result)
            => new SuccessResult(result);

        public static IResult Success()
            => new SuccessResult(null);

        public static IResult Copy(this IResult result)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));

            if (result is ErrorResult ErrorResultResult)
                return new ErrorResult(ErrorResultResult.Message, ErrorResultResult.Exception);

            if (result is SuccessResult successResult)
                return new SuccessResult(successResult.Result);

            return result;
        }

        public static string ReadErrorMessage(this IResult result)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));

            if (result.IsSuccess)
                throw new ArgumentException($"Attempted to read error message of a success result.");

            if (result is not ErrorResult errorResult)
                throw new ArgumentException($"The provided result type is not of ErrorResult.");

            return errorResult.Message;
        }

        public static Exception ReadException(this IResult result)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));

            if (result.IsSuccess)
                throw new ArgumentException($"Attempted to read exception of a success result.");

            if (result is not ErrorResult errorResult)
                throw new ArgumentException($"The provided result type is not of ErrorResult.");

            return errorResult.Exception;
        }

        public static bool TryReadValue(this IResult result, out object value)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));

            if (!result.IsSuccess)
            {
                value = null;
                return false;
            }

            value = result.Result;
            return true;
        }

        public static bool TryReadValue<TValue>(this IResult result, out TValue value)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result));

            if (!TryReadValue(result, out var objectResult))
            {
                value = default;
                return false;
            }

            if (objectResult is not TValue boxedValue)
            {
                value = default;
                return false;
            }

            value = boxedValue;
            return true;
        }

        public static bool TryReadValues<TValue>(this IEnumerable<IResult> results, out TValue[] values)
        {
            if (results is null)
                throw new ArgumentNullException(nameof(results));

            if (results.Count() <= 0)
            {
                values = Array.Empty<TValue>();
                return true;
            }

            var index = 0;
            var list = ListPool<TValue>.Shared.Rent();

            foreach (var result in results)
            {
                try
                {
                    if (!result.TryReadValue<TValue>(out var value))
                        continue;

                    list.Add(value);
                }
                catch { }

                index++;
            }

            values = ListPool<TValue>.Shared.ToArrayReturn(list);
            return true;
        }
    }
}
