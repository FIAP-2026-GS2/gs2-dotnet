using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SampaTech.Infrastructure.Persistence;

/// <summary>Grava a data como está (já em UTC) e, ao ler, marca o DateTime como UTC.</summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
