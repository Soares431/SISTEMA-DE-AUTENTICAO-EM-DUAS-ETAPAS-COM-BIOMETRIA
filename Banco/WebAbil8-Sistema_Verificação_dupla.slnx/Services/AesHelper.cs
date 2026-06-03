using System.Security.Cryptography;
using System.Text;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    // Helper AES usado para criptografar/descriptografar senhaClear no Int1.
    // Não está no Int4 (AesService) porque Int1 não pode referenciá-lo (Int4 já referencia Int1).
    public static class AesHelper
    {
        public static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string base64Cipher, string key)
        {
            var bytes = Convert.FromBase64String(base64Cipher);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

            var iv = bytes.Take(16).ToArray();
            var cipher = bytes.Skip(16).ToArray();
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
