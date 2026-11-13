namespace SampaTech.Domain.Enums;

// A ordem importa: a severidade de uma ocorrência só pode subir (LOW < ... < CRITICAL).
public enum Severity
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}
