namespace Jerry.Connection.Security;

public interface IEncDecryptor
{
    public byte[] EncryptOrDecrypt(byte[] Data);
}