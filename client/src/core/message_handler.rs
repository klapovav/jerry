use super::emulator::{Emulator, NoopEmulator};
use super::proto_in::{Button, Direction, HandshakeResult, State};
use super::{JerryMessage, JerryResponse};
use crate::configuration::{ScreenResolution, SessionParams};
use crate::emulation::JKey;
use crate::state::Command;
use arboard::Clipboard;
use enigo::MouseControllable;
use std::sync::mpsc::Sender;
use std::thread;
use std::time::{Duration, Instant};
use tap::TapFallible;
use tracing::{self, error, info, warn};

pub struct ContextAwareMessageHandler {
    transmitter: Sender<Command>,
    session_info: SessionParams,
    cursor: (i32, i32),
    pressed: [bool; 256],
    buttons: [bool; 5],
    state: ClientState,
    emulator: Box<dyn Emulator>,
    relative_move: bool,
    clipboard_client: Option<String>,
    clipboard_jerry: Option<String>,
    finished: bool,
    session: Instant,
}
#[derive(Clone, Copy, Debug)]
enum ClientState {
    Active,
    Inactive,
    None,
}
//
impl ContextAwareMessageHandler {
    pub fn new(transmitter: Sender<Command>, session_info: SessionParams) -> Self {
        let cursor = enigo::Enigo::new().mouse_location();
        let pressed: [bool; 256] = [false; 256];
        let buttons: [bool; 5] = [false; 5];
        //=============================================
        let emulator: Box<dyn Emulator> = match session_info.emulate_events {
            true => Box::new(Self::get_platform_emulator()),
            false => Box::new(NoopEmulator::new()),
        };
        //=============================================
        ContextAwareMessageHandler {
            transmitter,
            session_info,
            cursor,
            pressed,
            buttons,
            emulator,
            state: ClientState::None,
            clipboard_client: None,
            clipboard_jerry: None,
            relative_move: false,
            finished: false,
            session: Instant::now(),
        }
    }
    #[cfg(target_os = "windows")]
    fn get_platform_emulator() -> impl Emulator {
        super::emulator::WindowsImpl::new()
    }
    #[cfg(target_os = "linux")]
    fn get_platform_emulator() -> impl Emulator {
        super::emulator::LinuxImpl::new()
    }
    #[cfg(target_os = "macos")]
    fn get_platform_emulator() -> impl Emulator {
        super::emulator::MacImpl::new()
    }
    fn try_get_clip(&self) -> Option<String> {
        //thread::sleep(Duration::from_secs(1)); //DEBUGSERVER
        let ctx = Clipboard::new().tap_err(|e| error!("Clipboard::new() failed {:?}", e));

        match ctx {
            Ok(mut clip) => clip.get_text().ok(),
            Err(_) => None,
        }
    }
    fn set_clipboard(&self, content: String) -> Result<(), ProcessingError> {
        Clipboard::new()
            .and_then(|mut a| a.set_text(content))
            .map_err(|_| ProcessingError::FailedToProcess)
    }
    fn clear_state(&mut self, relative: bool) -> bool {
        self.relative_move = relative;
        self.clipboard_client = self.try_get_clip();

        for i in 0..255 {
            self.pressed[i] = false;
        }

        true
    }
    fn recover(&mut self) {
        self.pressed
            .into_iter()
            .enumerate()
            .filter(|(_, v)| *v)
            .for_each(|(i, _)| self.inject_release(i as u32));

        self.buttons
            .into_iter()
            .enumerate()
            .filter(|(_, v)| *v)
            .for_each(|(i, _)| self.inject_release_button(self.get_mouse_button(i)));
    }
    fn get_mouse_button(&self, value: usize) -> Button {
        match value {
            0 => Button::LEFT,
            1 => Button::RIGHT,
            2 => Button::MIDDLE,
            3 => Button::XBUTTON1,
            4 => Button::XBUTTON2,
            _ => panic!("Unknown usize value for proto_rs::Button"),
        }
    }
    fn inject_release(&mut self, code: u32) {
        let key = JKey::from(code as u8);
        let succ = self.emulator.key_up(code);
        let send_r = self
            .transmitter
            .send(Command::MessageCorrective(JerryMessage::Key(
                code,
                State::RELEASED,
            )));
        thread::sleep(Duration::from_millis(1));
        match (succ, send_r) {
            //self-recovery success,
            (Err(_), _) => warn!("Self-recovery: failed to release {:?}.", key),
            (Ok(_), Err(_)) => info!("Self-recovery: {:?} key released. [Program exit]", key),
            (Ok(_), Ok(_)) => info!("Self-recovery: {:?} key released", key),
        };
    }
    fn inject_release_button(&mut self, btn: Button) {
        _ = self.emulator.mouse_up(btn);
        _ = self
            .transmitter
            .send(Command::MessageCorrective(JerryMessage::MouseClick(
                btn,
                State::RELEASED,
            )));
        thread::sleep(Duration::from_millis(1));
    }
    fn key_down(&mut self, key: u32) -> Result<(), ProcessingError> {
        match self.state {
            ClientState::Active => {
                let key_u = key as usize;
                if key_u < self.pressed.len() {
                    return self
                        .emulator
                        .key_down(key)
                        .map(|_| self.pressed[key as usize] = true);
                }
                Err(ProcessingError::UnexpectedMessageDiscarded)
            }
            _ => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }
    fn key_up(&mut self, key: u32) -> Result<(), ProcessingError> {
        let key_u = key as usize;
        match self.pressed.get(key_u) {
            Some(true) => self
                .emulator
                .key_up(key)
                .map(|_| self.pressed[key_u] = false),
            Some(false) => Err(ProcessingError::UnexpectedMessageDiscarded),
            None => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }

    fn mouse_down(&mut self, btn: Button) -> Result<(), ProcessingError> {
        match self.state {
            ClientState::Active => self
                .emulator
                .mouse_down(btn)
                .map(|_| self.buttons[btn as usize] = true),
            _ => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }

    fn mouse_up(&mut self, btn: Button) -> Result<(), ProcessingError> {
        let button = btn as usize;
        match self.buttons.get(button) {
            Some(true) => self
                .emulator
                .mouse_up(btn)
                .map(|_| self.buttons[button] = false),
            Some(false) => Err(ProcessingError::UnexpectedMessageDiscarded),
            None => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }
    fn mouse_move(&mut self, x: i32, y: i32) -> Result<(), ProcessingError> {
        match (self.state, self.relative_move) {
            (ClientState::Active, true) => self.emulator.mouse_move_rel(x, y),
            (ClientState::Active, false) => self.emulator.mouse_move_to(x, y),
            (_, _) => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }

    fn mouse_wheel(&mut self, direction: Direction, amount: i32) -> Result<(), ProcessingError> {
        match self.state {
            ClientState::Active => self.emulator.mouse_wheel(direction, amount as f32),
            _ => Err(ProcessingError::UnexpectedMessageDiscarded),
        }
    }

    fn get_response(&self, request: &super::Request) -> Result<JerryResponse, ProcessingError> {
        match request {
            super::Request::INIT_INFO => {
                //thread::sleep(Duration::from_secs(30)); //DEBUGSERVER
                Ok(JerryResponse::InitInfo(self.session_info.clone()))
            }
            super::Request::CLIPBOARD => {
                if self.session.elapsed() < Duration::from_millis(500) {
                    // info!("Jerry clip content lenght: {}", new_content.len());
                    return Ok(JerryResponse::NoResponse(String::from("")));
                }
                match self.try_get_clip() {
                    Some(new_content) => {
                        info!("Clipboard content: \t\tLength: {}", new_content.len());
                        Ok(JerryResponse::Clipboard(new_content, false))
                    }
                    None => Ok(JerryResponse::NoResponse(String::from(""))),
                }
            }
            super::Request::MOUSE_POSITION => {
                match self.session_info.monitor {
                    ScreenResolution::Static(_) => {}
                    ScreenResolution::Dynamic => todo!(),
                }
                Ok(JerryResponse::Cursor(self.cursor.0, self.cursor.1))
            }
        }
    }
}

impl super::MessageConsumer for ContextAwareMessageHandler {
    fn finished(&self) -> bool {
        self.finished
    }
    fn consume(&mut self, msg: JerryMessage) -> Option<JerryResponse> {
        let (response, result) = match &msg {
            JerryMessage::MouseMove(x, y) => (None, self.mouse_move(*x, *y)),
            JerryMessage::Key(key, State::PRESSED) => (None, self.key_down(*key)),
            JerryMessage::Key(key, State::RELEASED) => (None, self.key_up(*key)),
            JerryMessage::MouseClick(btn, State::PRESSED) => (None, self.mouse_down(*btn)),
            JerryMessage::MouseClick(btn, State::RELEASED) => (None, self.mouse_up(*btn)),
            JerryMessage::MouseWheel(dir, amount) => (None, self.mouse_wheel(*dir, *amount)),
            JerryMessage::Request(req) => match self.get_response(req) {
                Ok(response) => (Some(response), Ok(())),
                Err(e) => (None, Err(e)),
            },
            JerryMessage::SessionBegin { relative_move: rel } => {
                if let ClientState::Active = self.state {
                    (None, Err(ProcessingError::UnexpectedMessageDiscarded))
                } else {
                    self.session = Instant::now();
                    self.clear_state(*rel);
                    self.state = ClientState::Active;
                    (None, Ok(()))
                }
            }
            JerryMessage::SessionEnd => match self.state {
                ClientState::Active => {
                    self.state = ClientState::Inactive;
                    self.recover();
                    self.clipboard_jerry = None;
                    if let Some(text) = self.clipboard_client.clone() {
                        let _res = self.set_clipboard(text);
                    }
                    (None, Ok(()))
                }
                _ => (None, Err(ProcessingError::UnexpectedMessageDiscarded)),
            },
            JerryMessage::Handshake(res, mess) => {
                if *res == HandshakeResult::Rejection {
                    self.transmitter
                        .send(Command::ConnectionResult(
                            crate::connection::ConnectionState::HandshakeFailed(mess.clone()),
                        ))
                        .unwrap_or_else(|_| self.recover());
                    self.transmitter
                        .send(Command::Halt)
                        .unwrap_or_else(|_| self.recover());
                } else {
                    self.transmitter
                        .send(Command::ConnectionResult(
                            crate::connection::ConnectionState::HandshakeSuccess(mess.clone()),
                        ))
                        .unwrap_or_else(|_| self.recover());
                }
                (None, Ok(()))
            }
            JerryMessage::Clipboard(content, file) => {
                if !file {
                    self.clipboard_jerry = Some(content.clone());
                    let a = Clipboard::new()
                        .and_then(|mut a| a.set_text(content))
                        .map_err(|_e| ProcessingError::FailedToProcess);
                    (None, a)
                } else {
                    (None, Err(ProcessingError::UnexpectedMessageDiscarded))
                }
            }
            JerryMessage::Heartbeat => (None, Ok(())),
        };
        if let Err(_ee) = self.transmitter.send(Command::Message(msg)) {
            //.unwrap_or_else(|_| self.recover())
            self.recover();
            self.finished = true;
        };
        if let Err(e) = result {
            match e {
                ProcessingError::UnexpectedMessageDiscarded => {}
                ProcessingError::UnableToProcess => warn!(
                    "Emulation failure: Unable to emulate input based on provided data. {:?}",
                    e
                ),
                ProcessingError::UnableToProcessPlatformSpecific(description) => warn!(
                    "Emulation failure: Unable to emulate input based on provided data. {:?}",
                    description
                ),
                ProcessingError::FailedToProcess => warn!(
                    "Emulation process error: Action failed to execute emulation function. {:?}",
                    e
                ),
            }
        }

        response
    }
}

#[allow(dead_code)]
#[derive(Debug, Clone)]
pub enum ProcessingError {
    UnexpectedMessageDiscarded,
    UnableToProcess,
    UnableToProcessPlatformSpecific(String),
    FailedToProcess,
}
