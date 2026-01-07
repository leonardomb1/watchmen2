using Microsoft.AspNetCore.DataProtection;

namespace Watchmen.Common.Services;

public class DataProtectionEncryptionService(IDataProtectionProvider provider) : IDataEncryptionService
{
    private readonly IDataProtectionProvider provider = provider;

    public string Encrypt(string plainText, string purpose)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Purpose cannot be null or empty", nameof(purpose));

        var protector = provider.CreateProtector(purpose);
        return protector.Protect(plainText);
    }

    public string Decrypt(string cipherText, string purpose)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Purpose cannot be null or empty", nameof(purpose));

        var protector = provider.CreateProtector(purpose);
        return protector.Unprotect(cipherText);
    }
}
