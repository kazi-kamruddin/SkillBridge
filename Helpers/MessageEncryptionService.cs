using System;
using System.Security.Cryptography;
using System.Text;

namespace SkillBridge.Helpers
{
    public static class MessageEncryptionService
    {
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("mysupersecretkeymysupersecretkey");

        public static (byte[] Ciphertext, byte[] IV, byte[] Hmac) Encrypt(string plainText)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = EncryptionKey; 
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    using (var hmac = new HMACSHA256(EncryptionKey))
                    {
                        var hmacBytes = hmac.ComputeHash(cipherBytes);
                        return (cipherBytes, aes.IV, hmacBytes);
                    }
                }
            }
        }

        public static string Decrypt(byte[] cipherText, byte[] iv, byte[] hmac)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = EncryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }
    }
}
