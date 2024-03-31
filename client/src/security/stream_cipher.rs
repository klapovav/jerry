use super::ChaChaKey;
use chacha20::cipher::{KeyIvInit, StreamCipher};
use chacha20::ChaCha20;
use std::io::{Read, Write};
use tracing::error;
pub struct Decryptor {
    encoded: std::net::TcpStream,
    cipher: ChaCha20,
}

impl Decryptor {
    pub fn new(stream: std::net::TcpStream, key: ChaChaKey) -> Self {
        let dec = ChaCha20::new(&key.key.into(), &key.nonce.into());
        Self {
            encoded: stream,
            cipher: dec,
        }
    }
}

impl Read for Decryptor {
    fn read(&mut self, buf: &mut [u8]) -> std::io::Result<usize> {
        let mut a = vec![0u8; buf.len()];
        let tmp = a.as_mut_slice();
        let res = self.encoded.read(tmp)?;
        if res == 0 {
            return Ok(0);
        }
        return match tmp.chunks_mut(res).next() {
            Some(res_out) => {
                self.cipher.apply_keystream(res_out);
                buf[..res].clone_from_slice(res_out);
                Ok(res)
            }
            None => Err(std::io::Error::from(std::io::ErrorKind::Other)),
        };
    }
}

pub struct Encryptor {
    encoded: std::net::TcpStream,
    cipher: ChaCha20,
}

impl Encryptor {
    pub fn new(stream: std::net::TcpStream, key: ChaChaKey) -> Self {
        let enc = ChaCha20::new(&key.key.into(), &key.nonce.into());
        Self {
            encoded: stream,
            cipher: enc,
        }
    }
}

impl Write for Encryptor {
    fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
        let mut buf_mut = buf.to_vec();
        let slice = &mut buf_mut[..];
        self.cipher.try_apply_keystream(slice).map_err(|e| {
            error!("Key stream error: {:?}", e);
            std::io::Error::from(std::io::ErrorKind::Other)
        })?;
        let res = self.encoded.write(slice)?;
        self.flush()?;
        Ok(res)
    }

    fn flush(&mut self) -> std::io::Result<()> {
        self.encoded.flush()
    }
}
