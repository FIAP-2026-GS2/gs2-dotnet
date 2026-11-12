namespace SampaTech.Domain.Exceptions;

/// <summary>Violação de uma regra de negócio do domínio.</summary>
public class DomainException(string message) : Exception(message);
