using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Watchmen.Modules.Companies;

namespace Watchmen.Modules.Persons;

[Table("person_companies")]
public sealed class PersonCompanyModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InternalId { get; private set; }

    public int PersonInternalId { get; set; }

    public int CompanyInternalId { get; set; }

    [ForeignKey(nameof(PersonInternalId))]
    public PersonModel Person { get; set; } = null!;

    [ForeignKey(nameof(CompanyInternalId))]
    public CompanyModel Company { get; set; } = null!;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}