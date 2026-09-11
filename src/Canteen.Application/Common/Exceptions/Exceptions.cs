namespace Canteen.Application.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public AppException(string message, int statusCode = 400, string errorCode = "BAD_REQUEST") 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} ({key}) was not found.", 404, "NOT_FOUND")
    {
    }
}

public class CapacityExceededException : AppException
{
    public CapacityExceededException(string message) 
        : base(message, 409, "CAPACITY_EXCEEDED")
    {
    }
}

public class ConcurrencyConflictException : AppException
{
    public ConcurrencyConflictException(string message = "The resource was modified by another request. Please retry.") 
        : base(message, 409, "CONCURRENCY_CONFLICT")
    {
    }
}

public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures have occurred.", 422, "VALIDATION_FAILED")
    {
        Errors = errors;
    }
}
