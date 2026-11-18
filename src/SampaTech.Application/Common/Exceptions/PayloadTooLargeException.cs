namespace SampaTech.Application.Common.Exceptions;

/// <summary>O arquivo enviado passa do tamanho máximo aceito. Vira HTTP 413.</summary>
public class PayloadTooLargeException(string message) : Exception(message);
