using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Watchmen.Modules.Vehicles;

[Table("vehicles")]
public sealed class VehicleModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InternalId { get; private set; }

    public Guid PublicId { get; init; } = Guid.NewGuid();

    [Required, MaxLength(20)]
    public string LicensePlate { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Model { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Color { get; set; } = null!;

    [Required]
    public VehicleType Type { get; set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}