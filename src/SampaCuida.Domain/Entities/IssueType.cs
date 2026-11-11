using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Domain.Entities;

/// <summary>
/// Tipo de problema do catálogo da prefeitura (ex.: "Buraco no asfalto").
/// A categoria é fixa e o código depende dela; o tipo é um dado gerenciável.
/// </summary>
public class IssueType
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 200;

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public IssueCategory Category { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private IssueType() { }

    public IssueType(string name, IssueCategory category, string? description = null)
    {
        Update(name, category, description);
        IsActive = true;
    }

    public void Update(string name, IssueCategory category, string? description)
    {
        Name = Guard.RequiredText(name, "nome", NameMaxLength);
        Category = Guard.DefinedEnum(category, "categoria");
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
    }

    // Desativar em vez de apagar preserva o histórico. Repetir a chamada não muda nada.
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
