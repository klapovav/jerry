use super::super::Command;
use crate::connection::ConnectionState;
use crate::{core::JerryMessage, emulation::JKey};
use std::sync::mpsc::Receiver;
use tracing::{debug, error, info, trace, warn};
pub struct View {
    receiver: Receiver<Command>,
}
impl View {
    pub fn new(rx: Receiver<Command>) -> Self {
        Self { receiver: rx }
    }
    pub fn run(&mut self) {
        loop {
            if let Ok(msg) = self.receiver.recv() {
                match msg {
                    Command::Draw => {}
                    Command::Message(msg) => self.process(msg, false),
                    Command::MessageCorrective(msg) => self.process(msg, true),
                    Command::ConnectionResult(st) => self.log(st),
                    Command::ExitWithError(error) => {
                        error!("Received a command to exit due to an error {:?}", error);
                        break;
                    }
                    Command::Halt => {
                        info!("Received a command to exit.");
                        break;
                    }
                }
            } else {
                debug!("Sender has disconnected, receiver can no longer receive messages");
                break;
            }
        }
    }

    fn process(&self, msg: crate::core::JerryMessage, correction: bool) {
        match msg {
            JerryMessage::MouseMove(x, y) => trace!("Mouse move {:?} x {:?}", x, y),
            JerryMessage::Key(code, state) => match correction {
                true => debug!(
                    "Self-recovery message: key {:?} {:?}",
                    JKey::from(code as u8),
                    state
                ),
                false => debug!(
                    "Key {:?}  {:?} (code: {:?})",
                    JKey::from(code as u8),
                    state,
                    code
                ),
            },
            JerryMessage::MouseClick(btn, state) => match correction {
                true => debug!("Self-recovery message: mouse button {:?} {:?}", btn, state),
                false => trace!("Mouse button {:?} {:?}", btn, state),
            },
            JerryMessage::MouseWheel(dir, amount) => trace!("Mouse wheel {:?} {:?}", dir, amount),
            JerryMessage::Handshake(res, message) => match message.len() {
                0 => info!("Connection result: {:?}", res),
                _ => warn!("Connection result: {:?}, description: {}", res, message),
            },
            JerryMessage::SessionBegin { relative_move: r } => match r {
                true => info!("Activated [relative movement]"),
                false => info!("Activated [absolute movement]"),
            },
            JerryMessage::SessionEnd => info!("Deactivated"),

            JerryMessage::Clipboard(content, _filelist) => {
                debug!("New clipboard content: {} ", content);
                info!("New clipboard content length: \t\t\t{} ", content.len())
            }
            JerryMessage::Request(a) => debug!("Request message: {:?}", a),
            JerryMessage::Heartbeat => {}
        }
    }
    fn log(&self, st: ConnectionState) {
        match st {
            ConnectionState::None
            | ConnectionState::Establishing
            | ConnectionState::Connected
            | ConnectionState::HandshakeSuccess(_)
            | ConnectionState::ConnectionError(_)
            | ConnectionState::ConnectedSecured => {
                info!("Connection result: {}", st)
            }
            ConnectionState::KeyExchangeFailed(_) | ConnectionState::HandshakeFailed(_) => {
                error!("Connection result: {}", st)
            }
            ConnectionState::ReadError(_) => warn!("Connection result: {}", st),
        }
    }
}
