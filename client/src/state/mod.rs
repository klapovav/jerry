pub mod log;
pub mod ui;
use crate::connection::ConnectionState;
use crate::core::JerryMessage;

#[derive(Clone, Copy, PartialEq, Debug)]
pub struct Coord {
    pub x: i32,
    pub y: i32,
}

#[allow(dead_code)]
pub enum Command {
    Draw,
    Message(JerryMessage),
    MessageCorrective(JerryMessage),
    ConnectionResult(ConnectionState),
    Halt,
    ExitWithError(String),
}
