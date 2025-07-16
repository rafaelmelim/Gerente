namespace Gerente.Models
{
    public class ConfiguracaoEmail
    {
        public int Id { get; set; }
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Porta { get; set; }
        public string EmailRemetente { get; set; } = string.Empty;
        public string NomeRemetente { get; set; } = string.Empty;
        public string UsuarioSmtp { get; set; } = string.Empty;
        public string SenhaSmtp { get; set; } = string.Empty;
        public string SecurityMode { get; set; } = "None";
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }

    public static class CryptoUtils
    {
        // Chave e IV fixos para exemplo. Em produção, use armazenamento seguro!
        private static readonly byte[] Key = new byte[32] { 21, 72, 13, 44, 55, 16, 77, 88, 19, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
        private static readonly byte[] IV = new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return System.Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                    return "";

                byte[] fullCipher;
                try
                {
                    fullCipher = System.Convert.FromBase64String(cipherText);
                }
                catch (FormatException)
                {
                    // Não é Base64, pode ser texto puro antigo
                    return cipherText;
                }

                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = Key;
                aes.IV = IV;
                aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new System.IO.MemoryStream(fullCipher);
                using var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
                using var sr = new System.IO.StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Erro ao descriptografar: {ex.Message} | Valor: {cipherText}");
#endif
                // Fallback: retorna string vazia para não quebrar o sistema
                return "";
            }
        }
    }
} 