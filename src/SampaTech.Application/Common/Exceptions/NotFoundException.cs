namespace SampaTech.Application.Common.Exceptions;

/// <summary>O recurso pedido na rota não existe. Vira HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);
