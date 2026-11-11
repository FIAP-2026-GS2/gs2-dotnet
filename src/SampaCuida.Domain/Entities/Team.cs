using SampaCuida.Domain.Common;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>Equipe de campo de um órgão da prefeitura. Não guarda dados dos integrantes.</summary>
public class Team
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 200;

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public int MunicipalAgencyId { get; private set; }
    public MunicipalAgency? MunicipalAgency { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private Team() { }

    public Team(string name, int municipalAgencyId, string? description = null)
    {
        Update(name, municipalAgencyId, description);
        IsActive = true;
    }

    public void Update(string name, int municipalAgencyId, string? description)
    {
        // O Domain só confere o formato; se o órgão existe, quem confere é o Application.
        if (municipalAgencyId <= 0)
            throw new DomainException("O órgão da prefeitura é obrigatório.");

        Name = Guard.RequiredText(name, "nome", NameMaxLength);
        MunicipalAgencyId = municipalAgencyId;
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
    }

    // Desativar em vez de apagar preserva o histórico. Repetir a chamada não muda nada.
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
