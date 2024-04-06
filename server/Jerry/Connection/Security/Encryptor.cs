using Microsoft.VisualBasic.Logging;
using System.Linq;
using Serilog;

namespace Jerry.Connection.Security;

public class Encryptor : IEncDecryptor
{
    private readonly ChaCha20 encryptor;

    public Encryptor(Agreement secret)
    {
        encryptor = new ChaCha20(secret.Key, 0, secret.IV[..12]);
    }

    public byte[] EncryptOrDecrypt(byte[] Data)
    {
        return encryptor.EncryptOrDecrypt(Data);
    }
}