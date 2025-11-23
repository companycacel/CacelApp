using System.Security.Cryptography;
using System.Text;

namespace Core.Shared.Helpers;

/// <summary>
/// Helper para encriptar y desencriptar passwords de forma segura
/// </summary>
public static class PasswordEncryption
{
    private static readonly byte[] _key = Encoding.UTF8.GetBytes("CacelApp2024Key!");
    private static readonly byte[] _iv = Encoding.UTF8.GetBytes("CacelApp2024Iv!!");
    
    /// <summary>
    /// Encripta un texto plano
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;
            
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(cipherBytes);
    }
    
    /// <summary>
    /// Desencripta un texto encriptado
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;
            
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            
            var cipherBytes = Convert.FromBase64String(cipherText);
            
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}
