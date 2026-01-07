using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Watchmen.Modules.Companies;

[Table("companies")]
public sealed class CompanyModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InternalId { get; private set; }

    public Guid PublicId { get; init; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Address { get; set; } = null!;

    [Required, MaxLength(40)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required, MaxLength(100)]
    public string ContactPerson { get; set; } = null!;

    [Required, MaxLength(20)]
    public string FiscalCode { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}