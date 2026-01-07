namespace Watchmen.Common.Services;

public interface IDataEncryptionService
{
    string Encrypt(string plainText, string purpose);
    string Decrypt(string cipherText, string purpose);
}
