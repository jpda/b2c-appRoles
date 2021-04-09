using System;

namespace B2CAuthZ.Admin
{
    public class ServiceResult<T> : ServiceResult
    {
        public T Value { get; set; }
        public ServiceResult() { }

        public ServiceResult(T value)
        {
            Value = value;
            Success = true;
        }

        public static ServiceResult<T> FromError(string error)
        {
            return new ServiceResult<T>()
            {
                Message = error,
                Success = false,
                Exception = new Exception(error)
            };
        }
        public static ServiceResult<T> FromError(Exception ex)
        {
            return new ServiceResult<T>()
            {
                Message = ex.Message,
                Success = false,
                Exception = ex
            };
        }

        public static ServiceResult<T> FromResult(T thing)
        {
            return new ServiceResult<T>(thing);
        }

        public static ServiceResult<T> FromError(string message, T value)
        {
            var result = FromError(message);
            result.Value = value;
            return result;
        }

        public static ServiceResult<T> FromError(Exception ex, T value)
        {
            var result = FromError(ex);
            result.Value = value;
            return result;
        }
    }

    public abstract class ServiceResult
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }
}