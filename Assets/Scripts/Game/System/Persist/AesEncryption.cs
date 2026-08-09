using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AesEncryption
{
    // 固定盐值（用于密钥派生，也可随机生成并保存）
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("TinyRpg-BigSmall"); // 实际项目中请更换

    /// <summary>
    ///     加密字节数组
    /// </summary>
    private static byte[] Encrypt(byte[] dataToEncrypt, string password)
    {
        using var aes = Aes.Create();
        // 使用 Rfc2898DeriveBytes 从密码派生密钥和 IV
        using (var deriveBytes = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256))
        {
            aes.Key = deriveBytes.GetBytes(32); // AES-256 密钥 32 字节
            aes.IV = deriveBytes.GetBytes(16); // IV 16 字节
        }

        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    /// <summary>
    ///     解密字节数组
    /// </summary>
    private static byte[] Decrypt(byte[] dataToDecrypt, string password)
    {
        using var aes = Aes.Create();
        // 密钥派生必须与加密时完全一致
        using (var deriveBytes = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256))
        {
            aes.Key = deriveBytes.GetBytes(32);
            aes.IV = deriveBytes.GetBytes(16);
        }

        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(dataToDecrypt, 0, dataToDecrypt.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    // ---------- 以下为扩展便捷方法（操作字符串） ----------
    public static string EncryptString(string plainText, string password)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = Encrypt(plainBytes, password);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptString(string cipherTextBase64, string password)
    {
        var cipherBytes = Convert.FromBase64String(cipherTextBase64);
        var plainBytes = Decrypt(cipherBytes, password);
        return Encoding.UTF8.GetString(plainBytes);
    }
}