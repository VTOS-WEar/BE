namespace VTOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity was not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException() : base() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException) { }

    public NotFoundException(string name, object key) 
        : base($"Entity \"{name}\" ({key}) was not found.") { }
}

/// <summary>
/// Exception thrown when rate limit is exceeded (429 Too Many Requests)
/// </summary>
public class TooManyRequestsException : Exception
{
    public TooManyRequestsException() : base() { }

    public TooManyRequestsException(string message) : base(message) { }

    public TooManyRequestsException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails (400 Bad Request)
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
