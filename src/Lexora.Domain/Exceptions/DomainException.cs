namespace Lexora.Domain.Exceptions;

/// <summary>
/// Base exception for all business domain errors in Lexora.
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when an expected entity cannot be found by its identifier or business key.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object? EntityKey { get; }

    public EntityNotFoundException(string entityName, object? entityKey)
        : base($"Entity '{entityName}' with identifier '{entityKey}' was not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }

    public EntityNotFoundException(string message) : base(message)
    {
        EntityName = string.Empty;
    }
}

/// <summary>
/// Thrown when an operation would violate unique business constraints (e.g. duplicate term or code).
/// </summary>
public class DuplicateEntityException : DomainException
{
    public string EntityName { get; }
    public string DuplicateKey { get; }

    public DuplicateEntityException(string entityName, string duplicateKey)
        : base($"Entity '{entityName}' with key '{duplicateKey}' already exists.")
    {
        EntityName = entityName;
        DuplicateKey = duplicateKey;
    }

    public DuplicateEntityException(string message) : base(message)
    {
        EntityName = string.Empty;
        DuplicateKey = string.Empty;
    }
}

/// <summary>
/// Thrown when an invalid state transition or parameter violates learning invariants.
/// </summary>
public class InvalidLearningStateException : DomainException
{
    public InvalidLearningStateException(string message) : base(message)
    {
    }

    public InvalidLearningStateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when entity input parameters fail domain validation rules.
/// </summary>
public class DomainValidationException : DomainException
{
    public string? PropertyName { get; }

    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string propertyName, string message) : base(message)
    {
        PropertyName = propertyName;
    }

    public DomainValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
