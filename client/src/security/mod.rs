pub mod key_exchange;
pub mod stream_cipher;
pub use stream_cipher::Decryptor;
pub use stream_cipher::Encryptor;
//ChaCha20 stream cipher (RFC 8439 version with 96-bit nonce)

#[derive(Clone, Copy)]
pub struct ChaChaKey {
    pub key: [u8; 32],
    pub nonce: [u8; 12],
}

pub struct KeyPair {
    pub key: [u8; 32],
    pub nonce: [u8; 32], //96 bit
}

impl From<KeyPair> for ChaChaKey {
    fn from(pair: KeyPair) -> Self {
        let mut iv_m12: [u8; 12] = [0; 12];
        iv_m12.copy_from_slice(&pair.nonce[..12]);
        ChaChaKey {
            key: pair.key,
            nonce: iv_m12,
        }
    }
}
