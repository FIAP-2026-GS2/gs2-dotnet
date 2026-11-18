namespace SampaTech.Application.Common.Exceptions;

/// <summary>
/// Um serviço de que a operação depende (IA, armazenamento de fotos) está fora do ar ou sem
/// configuração. Nada foi gravado; dá para tentar de novo. Vira HTTP 503.
/// </summary>
public class ServiceUnavailableException(string message) : Exception(message);
