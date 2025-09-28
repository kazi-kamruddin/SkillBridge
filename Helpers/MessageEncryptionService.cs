using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SkillBridge.Helpers
{
    public static class MessageEncryptionService
    {
        // ⚠️ Replace this with a secure key from config/env (32 bytes recommended)
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes(
            "ReplaceThisWithYour32ByteSecureKey!!"
        );

        public static (byte[] Ciphertext, byte[] IV, byte[] Hmac) Encrypt(string plainText)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    // Compute HMAC (for tamper detection)
                    using (var hmac = new HMACSHA256(EncryptionKey))
                    {
                        byte[] hmacValue = hmac.ComputeHash(cipherBytes);

                        return (cipherBytes, aes.IV, hmacValue);
                    }
                }
            }
        }

        public static string Decrypt(byte[] ciphertext, byte[] iv, byte[] hmacValue)
        {
            if (ciphertext == null || iv == null || hmacValue == null)
                throw new ArgumentNullException("Ciphertext, IV, and HMAC must all be provided");

            // Verify HMAC
            using (var hmac = new HMACSHA256(EncryptionKey))
            {
                byte[] computedHmac = hmac.ComputeHash(ciphertext);
                if (!AreEqual(computedHmac, hmacValue))
                    throw new CryptographicException("Message authentication failed!");
            }

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = EncryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
