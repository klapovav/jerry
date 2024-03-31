use super::KeyPair;
use rand_core::OsRng;
use std::io::{Error, Read, Write};
use std::net::TcpStream;
use x25519_dalek::{EphemeralSecret, PublicKey, SharedSecret as SharedSecret32};

pub fn key_nonce_agreement(stream: &mut TcpStream) -> Result<KeyPair, Error> {
    let key = match establish(stream) {
        Ok(shared) => shared.as_bytes().to_owned(),
        Err(e) => return Err(e),
    };
    let nonce = match establish(stream) {
        Ok(shared) => shared.as_bytes().to_owned(),
        Err(e) => return Err(e),
    };
    //info!("   SHARED key: {:?}", key);
    //info!("   SHARED nonce: {:?}", nonce);
    Ok(KeyPair { key, nonce })
}

pub fn establish(stream: &mut TcpStream) -> Result<SharedSecret32, Error> {
    let my_secret = EphemeralSecret::new(OsRng);
    let my_public = PublicKey::from(&my_secret);
    stream.write_all(my_public.as_bytes())?;
    stream.flush()?;
    let mut bob_public: [u8; 32] = [0; 32];
    stream.read_exact(&mut bob_public)?;
    Ok(my_secret.diffie_hellman(&PublicKey::from(bob_public)))
}

pub fn get_secrets_chacha(stream: &mut TcpStream) -> Option<(super::ChaChaKey, super::ChaChaKey)> {
    let m_r = crate::security::key_exchange::key_nonce_agreement(stream);
    let s_r = crate::security::key_exchange::key_nonce_agreement(stream);
    if let (Ok(m), Ok(s)) = (m_r, s_r) {
        let master = super::ChaChaKey::from(m);
        let slave = super::ChaChaKey::from(s);
        return Some((master, slave));
    }
    None
}
