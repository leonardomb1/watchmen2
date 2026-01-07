using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Watchmen.Modules.Users;

[Table("users")]
public sealed class UserModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InternalId { get; private set; }

    public Guid PublicId { get; init; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Email { get; set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    private static readonly PasswordHasher<UserModel> passwordHasher = new();

    public void SetPassword(string password)
    {
        PasswordHash = passwordHasher.HashPassword(this, password);
    }

    public bool VerifyPassword(string password)
    {
        var result = passwordHasher.VerifyHashedPassword(this, PasswordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
