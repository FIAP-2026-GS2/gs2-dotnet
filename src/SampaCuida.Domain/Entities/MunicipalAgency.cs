using SampaCuida.Domain.Common;

namespace SampaCuida.Domain.Entities;

/// <summary>Órgão da prefeitura (ex.: uma subprefeitura) dono das equipes de campo.</summary>
public class MunicipalAgency
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int SubprefectureMaxLength = 100;

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string? Subprefecture { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private MunicipalAgency() { }

    public MunicipalAgency(
        string name,
        string city,
        string state,
        string? description = null,
        string? subprefecture = null)
    {
        Update(name, city, state, description, subprefecture);
        IsActive = true;
    }

    public void Update(string name, string city, string state, string? description, string? subprefecture)
    {
        Name = Guard.RequiredText(name, "nome", NameMaxLength);
        City = Guard.RequiredText(city, "cidade", CityMaxLength);
        State = Guard.StateCode(state);
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        Subprefecture = Guard.OptionalText(subprefecture, "subprefeitura", SubprefectureMaxLength);
    }

    // Desativar em vez de apagar preserva o histórico. Repetir a chamada não muda nada.
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
