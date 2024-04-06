using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Jerry.Connection.Security
{
    internal class ChaCha20
    {
        private readonly IStreamCipher cipher;

        public ChaCha20(byte[] key, int index, byte[] nonce)
        {
            var keyParamWithIV = new ParametersWithIV(new KeyParameter(key), nonce, index, nonce.Length);
            cipher = new ChaCha7539Engine();
            cipher.Init(true, keyParamWithIV);
        }

        public byte[] EncryptOrDecrypt(byte[] data)
        {
            byte[] processed = new byte[data.Length];
            cipher.ProcessBytes(data, 0, data.Length, processed, 0);

            //for (int j = 0; j < plainTextData.Length; j++)
            //{
            //    cipherTextData[j] = cipher.ReturnByte(plainTextData[j]);
            //}

            return processed;
        }
    }
}
