pub mod emulator;

pub mod message_handler;
pub use crate::state::Command;
//========================
//   CORE mod.rs
//========================

pub trait MessageConsumer {
    fn consume(&mut self, msg: JerryMessage) -> Option<JerryResponse>;
    fn finished(&self) -> bool;
}

use crate::configuration::SessionParams;
pub use crate::proto_rs::request_master::Button;
pub use crate::proto_rs::request_master::Direction;
pub use crate::proto_rs::request_master::Echo;
pub use crate::proto_rs::request_master::Request;
pub use crate::proto_rs::request_master::State;

pub use crate::proto_rs::*;

use self::proto_in::HandshakeResult;

//========================
//   JERRY STRUCTS
//========================

#[derive(Debug, Clone)]
pub enum JerryMessage {
    MouseMove(i32, i32),
    Key(u32, State),
    MouseClick(Button, State),
    MouseWheel(Direction, i32),
    SessionBegin { relative_move: bool },
    SessionEnd,
    Clipboard(String, bool),
    Request(Request),
    Handshake(HandshakeResult, String),
    Heartbeat,
}
#[derive(Debug)]
pub enum JerryResponse {
    Cursor(i32, i32),
    InitInfo(SessionParams),
    Clipboard(String, bool),
    NoResponse(String),
}
