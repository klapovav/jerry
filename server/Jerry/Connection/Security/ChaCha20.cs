namespace Jerry.Connection.Security
{
    // MOCK
    // TODO
    internal class ChaCha20
    {
        private readonly byte[] key;
        private readonly int index;
        private readonly byte[] nonce;

        public ChaCha20(byte[] key, int index, byte[] nonce)
        {
            this.key = key;
            this.index = index;
            this.nonce = nonce;
        }

        public byte[] EncryptOrDecrypt(byte[] Data) => Data;
    }
}