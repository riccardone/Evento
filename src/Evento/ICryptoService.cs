namespace Evento
{
    public interface ICryptoService
    {
        byte[] Encrypt(string plainText, string key);
        string Decrypt(byte[] cipherText, string key);
    }
}