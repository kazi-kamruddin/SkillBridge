using System;
using System.Security.Cryptography;
using System.Text;

namespace SkillBridge.Helpers
{
    public static class MessageEncryptionService
    {
        private static readonly byte[] MasterKey = ReadMasterKey();
        private static readonly byte[] EncryptionKey = DeriveKey("encryption");
        private static readonly byte[] AuthenticationKey = DeriveKey("authentication");

        public static (byte[] Ciphertext, byte[] IV, byte[] Hmac) Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentException("Message text is required.", nameof(plainText));

            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] ciphertext;
                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    ciphertext = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }

                return (ciphertext, aes.IV, ComputeHmac(aes.IV, ciphertext));
            }
        }

        public static string Decrypt(byte[] cipherText, byte[] iv, byte[] hmac)
        {
            if (cipherText == null || iv == null || hmac == null || iv.Length != 16 || hmac.Length != 32)
                throw new CryptographicException("Invalid message payload.");

            var expected = ComputeHmac(iv, cipherText);
            var difference = 0;
            for (var i = 0; i < expected.Length; i++)
                difference |= expected[i] ^ hmac[i];
            if (difference != 0)
                throw new CryptographicException("Message authentication failed.");

            using (var aes = Aes.Create())
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

        private static byte[] ReadMasterKey()
        {
            var encoded = Environment.GetEnvironmentVariable("SKILLBRIDGE_MESSAGE_KEY");
            if (string.IsNullOrWhiteSpace(encoded))
                throw new InvalidOperationException("Set SKILLBRIDGE_MESSAGE_KEY to a Base64-encoded 32-byte key before using chat.");

            byte[] key;
            try { key = Convert.FromBase64String(encoded); }
            catch (FormatException ex) { throw new InvalidOperationException("SKILLBRIDGE_MESSAGE_KEY must be valid Base64.", ex); }
            if (key.Length != 32)
                throw new InvalidOperationException("SKILLBRIDGE_MESSAGE_KEY must decode to exactly 32 bytes.");
            return key;
        }

        private static byte[] DeriveKey(string purpose)
        {
            using (var hmac = new HMACSHA256(MasterKey))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes("SkillBridge message " + purpose));
        }

        private static byte[] ComputeHmac(byte[] iv, byte[] ciphertext)
        {
            var payload = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, payload, iv.Length, ciphertext.Length);
            using (var hmac = new HMACSHA256(AuthenticationKey))
                return hmac.ComputeHash(payload);
        }
    }
}
