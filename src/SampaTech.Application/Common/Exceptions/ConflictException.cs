namespace SampaTech.Application.Common.Exceptions;

/// <summary>
/// A operação conflita com dados existentes (valor único repetido ou registro em uso). Vira HTTP 409.
/// </summary>
public class ConflictException(string message) : Exception(message);
