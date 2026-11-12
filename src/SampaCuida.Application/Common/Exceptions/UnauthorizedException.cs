namespace SampaCuida.Application.Common.Exceptions;

/// <summary>Credenciais inválidas. Vira HTTP 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
