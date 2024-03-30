namespace Jerry.Connection.Security;

public class Encryptor : IEncDecryptor
{
    private readonly ChaCha20 encoder;

    public Encryptor(Agreement secret)
    {
        encoder = new ChaCha20(secret.Key, 0, secret.IV[..12]);
    }

    public byte[] EncryptOrDecrypt(byte[] Data) => encoder.EncryptOrDecrypt(Data);
}