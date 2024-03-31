use crate::configuration::SessionParams;
use crate::core::message_handler::ContextAwareMessageHandler;
use crate::core::Command;
use std::error::Error;
use std::io::{ErrorKind, Read, Write};
use std::sync::mpsc::Sender;
use std::time::Duration;
use tracing::{debug, info, warn};
mod conn;
#[derive(PartialEq, Debug)]
pub enum ConnectionState {
    None,
    Establishing,
    ConnectionError(String),
    Connected,
    ConnectedSecured,
    KeyExchangeFailed(String),
    HandshakeFailed(String),
    HandshakeSuccess(String),
    ReadError(String),
}

use std::fmt;
impl fmt::Display for ConnectionState {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            ConnectionState::None => write!(f, "None"),
            ConnectionState::Establishing => write!(f, "Establishing"),
            ConnectionState::Connected => write!(f, "Connected"),
            ConnectionState::ConnectedSecured => write!(f, "Encrypted communication established"),
            ConnectionState::ConnectionError(_) => write!(f, "Connection error"),
            ConnectionState::ReadError(_s) => write!(f, "Read error"),
            ConnectionState::HandshakeFailed(s) => write!(f, "Handshake failed ({})", s),
            ConnectionState::HandshakeSuccess(s) => write!(f, "Handshake succeeded ({})", s),
            ConnectionState::KeyExchangeFailed(_) => write!(f, "Key exchange failed",),
        }
    }
}

pub struct ConnectionWorker {
    transmitter: Sender<Command>,
    info: SessionParams,
    cycle_count: u32,
}

const HEARTBEAT_TIMEOUT: Duration = Duration::from_millis(2_500);

impl ConnectionWorker {
    pub fn new(transmitter: Sender<Command>, info: SessionParams) -> Self {
        ConnectionWorker {
            transmitter,
            info,
            cycle_count: 0,
        }
    }
    pub fn run(&mut self) {
        let result = self.loop_connection_read();
        if let Err(err) = result {
            eprintln!("Connection worker error: {}", err);
        }
        let _ = self.transmitter.send(Command::Halt);
    }

    fn loop_connection_read(&mut self) -> Result<(), Box<dyn Error>> {
        'listen: loop {
            if !self.try_send_state(ConnectionState::Establishing) {
                break 'listen;
            }
            if self.cycle_count != 0 {
                std::thread::sleep(Duration::from_secs(1));
            }
            self.cycle_count += 1;

            let connection_res = conn::Connection::new(self.info.ip, self.info.port)
                .enable_automatic_reconnection(1)
                .set_timeout(5)
                .events_on_error(|err_kind| {
                    if err_kind == ErrorKind::ConnectionAborted {
                        return Some(err_kind);
                    }
                    None
                })
                .connect();

            if let Err(e) = connection_res {
                match self.try_send_state(ConnectionState::ConnectionError(e.to_string())) {
                    true => continue 'listen,
                    false => break 'listen,
                }
            }
            let mut stream = connection_res.unwrap();

            if stream.set_nodelay(true).is_err() {
                warn!("Nagle's algorithm is enabled");
            }
            if let Err(e) = stream.set_read_timeout(Some(HEARTBEAT_TIMEOUT)) {
                self.try_send_state(ConnectionState::ConnectionError(e.to_string()));
                break 'listen;
            }
            if !self.try_send_state(ConnectionState::Connected) {
                break 'listen;
            }

            let keys_option = crate::security::key_exchange::get_secrets_chacha(&mut stream);

            if keys_option.is_none() {
                // Exit
                let _ = self.try_send_state(ConnectionState::KeyExchangeFailed("".into()));
                break 'listen;
            }

            let (master, slave) = keys_option.unwrap();
            use crate::security::{Decryptor, Encryptor};

            let out = match stream.try_clone() {
                Ok(stream) => stream,
                Err(_) => continue 'listen,
            };
            let _ = out.set_nodelay(true);

            let (mut in_stream, mut out_stream): (Box<dyn Read>, Box<dyn Write>) =
                match crate::ENCRYPT {
                    true => {
                        let dec = Decryptor::new(stream, master);
                        let enc = Encryptor::new(out, slave);
                        (Box::new(dec), Box::new(enc))
                    }
                    false => (Box::new(stream), Box::new(out)),
                };
            let mut listener =
                crate::serialization::ProtoSerDe::new(&mut in_stream, &mut out_stream);

            if !self.try_send_state(ConnectionState::ConnectedSecured) {
                break 'listen;
            }

            let msg_handler: ContextAwareMessageHandler =
                ContextAwareMessageHandler::new(self.transmitter.clone(), self.info.clone());

            if let Err(e) = listener.listen_loop(Box::new(msg_handler)) {
                warn!("{}", e);
                if !self.try_send_state(ConnectionState::ReadError(e.to_string())) {
                    break 'listen;
                }
            }
            info!("Disconnected");
        }
        Ok(())
    }

    fn try_send_state(&self, state: ConnectionState) -> bool {
        let result = self.transmitter.send(Command::ConnectionResult(state));
        match result {
            Ok(_) => true,
            Err(error) => {
                debug!(
                    "Failed to send message. Receiver has already been deallocated. Error {error} "
                );
                false
            }
        }
    }
}
