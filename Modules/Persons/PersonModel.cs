using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Watchmen.Modules.Persons;

[Table("persons")]
public sealed class PersonModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InternalId { get; private set; }

    public Guid PublicId { get; init; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(500)]
    public string DocumentNumber { get; set; } = null!;

    [Required, MaxLength(64)]
    public string DocumentNumberHash { get; set; } = null!;

    [MaxLength(500)]
    public string? Email { get; set; }

    [MaxLength(64)]
    public string? EmailHash { get; set; } = null!;

    [MaxLength(500)]
    public string? PhoneNumber { get; set; }

    [MaxLength(64)]
    public string? PhoneNumberHash { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}