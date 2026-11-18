namespace SampaTech.Application.Common.Exceptions;

/// <summary>A IA conferiu a foto e ela não serve como prova (ex.: selfie, animal, print de tela). Vira HTTP 422.</summary>
public class PhotoRejectedException(string message) : Exception(message);
