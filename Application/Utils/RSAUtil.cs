using System.Security.Cryptography;
using System.Text;

namespace Application.Utils;

public class RSAUtil
{
    private const int PublicKeySize = 2048;

    public static (string PublicKey, string PrivateKey) GenerateKeyPair()
    {
        using var rsa = new RSACryptoServiceProvider(PublicKeySize);
        return (Convert.ToBase64String(rsa.ExportRSAPublicKey()), Convert.ToBase64String(rsa.ExportRSAPrivateKey()));
    }

    public static string Decrypt(string privateKey, string data)
    {
        using var rsa = new RSACryptoServiceProvider(PublicKeySize);
        var privateKeyBytes = Convert.FromBase64String(privateKey);
        var dataBytes = Convert.FromBase64String(data);
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        var decryptedBytes = rsa.Decrypt(dataBytes, true);
        var decryptedString = Encoding.UTF8.GetString(decryptedBytes);
        return decryptedString;
    }

    public static string Encrypt(string publicKey, string data)
    {
        using var rsa = new RSACryptoServiceProvider(PublicKeySize);
        var publicKeyBytes = Convert.FromBase64String(publicKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        var encryptedBytes = rsa.Encrypt(dataBytes, true);
        return Convert.ToBase64String(encryptedBytes);
    }
}