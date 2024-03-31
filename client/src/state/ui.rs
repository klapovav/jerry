use num::Integer;
use ratatui::backend::Backend;
use ratatui::layout::{Constraint, Direction, Layout};
use ratatui::style::{Color, Style};
use ratatui::text::Span;
use ratatui::widgets::block::Title;
use ratatui::widgets::canvas::Canvas;
use ratatui::widgets::{Block, BorderType, Borders};
use ratatui::{symbols, Terminal};
use tracing::info;

use std::sync::mpsc::{Receiver, Sender};
use std::time::Duration;

use super::JerryMessage;
use super::{Command, Coord};

use crate::connection::ConnectionState;
use crate::proto_rs::proto_in::State;

const TICK_INTERVAL: Duration = Duration::from_millis(50);

pub struct WindowState<'a, B: Backend> {
    receiver: Receiver<Command>,
    transmitter: Sender<Command>,

    terminal: &'a mut Terminal<B>,

    connection_state: ConnectionState,
    pub mon_size: Coord,
    pub cursor: Coord,
    active: bool,
    rendering_pause_cycles: u16,
    keys: Vec<u32>,
    wheel: char,
    mouse_btn: i16,
    // _clip: String,
    // _clip_len: usize,
    heart: u8,
    relative_move: bool,
}
impl<'a, B: Backend> WindowState<'a, B> {
    pub fn new(
        mon_size: Coord,
        tx: Sender<Command>,
        rx: Receiver<Command>,
        terminal: &'a mut Terminal<B>,
    ) -> WindowState<B> {
        WindowState {
            receiver: rx,
            transmitter: tx,
            terminal,
            mon_size,
            connection_state: ConnectionState::None,
            active: false,
            cursor: Coord { x: 0, y: 0 },
            rendering_pause_cycles: 0,
            keys: Vec::new(),
            wheel: ' ',
            mouse_btn: 0,
            // _clip_len: 0,
            // _clip: String::new(),
            heart: 0,
            relative_move: false,
        }
    }

    pub fn run(&mut self) {
        let tx = self.transmitter.clone();

        let _tick_handle = std::thread::spawn(move || {
            loop {
                std::thread::sleep(TICK_INTERVAL);
                if tx.send(Command::Draw).is_err() {
                    break;
                }
            }
            drop(tx);
        });

        _ = self.terminal.clear();
        loop {
            if let Ok(msg) = self.receiver.recv() {
                match msg {
                    Command::Draw => self.render(),
                    Command::Message(msg) => self.process(msg),
                    Command::MessageCorrective(msg) => self.process(msg),
                    Command::ConnectionResult(st) => {
                        self.connection_state = st;
                        self.render();
                        self.pause_rendering(10);
                    }
                    Command::Halt => break,
                    Command::ExitWithError(error) => {
                        tracing::error!("{}", error);
                        break;
                    }
                }
            } else {
                info!("Sender has disconnected, receiver can no longer receive messages");
                break;
            }
        }
    }
    fn process(&mut self, msg: JerryMessage) {
        match msg {
            JerryMessage::MouseMove(x, y) => {
                if self.relative_move {
                    self.cursor.x = self.mon_size.x / 2 + x;
                    self.cursor.y = self.mon_size.y / 2 + y;
                } else {
                    self.cursor = super::Coord { x, y }
                }
            }
            JerryMessage::Key(code, State::PRESSED) => {
                if !self.keys.contains(&code) {
                    self.keys.push(code);
                }
            }
            JerryMessage::Key(code, State::RELEASED) => {
                if self.keys.contains(&code) {
                    self.keys.retain(|&x| x != code);
                }
            }
            JerryMessage::MouseClick(_, s) => match s {
                State::PRESSED => {
                    //self.heart = 1;
                    self.mouse_btn += 1
                }
                State::RELEASED => self.mouse_btn -= 1,
            },
            JerryMessage::MouseWheel(dir, _amount) => {
                //let _abs = if amount < 0 { -amount } else { amount };
                self.wheel = match dir {
                    crate::proto_rs::proto_in::Direction::SCROLL_UP => '^',
                    crate::proto_rs::proto_in::Direction::SCROLL_DOWN => 'v',
                    crate::proto_rs::proto_in::Direction::SCROLL_LEFT => '<',
                    crate::proto_rs::proto_in::Direction::SCROLL_RIGHT => '>',
                }
            }
            JerryMessage::SessionBegin {
                relative_move: relative,
            } => {
                self.active = true;
                self.relative_move = relative;
            }
            JerryMessage::SessionEnd => self.active = false,
            JerryMessage::Clipboard(content, _) => {
                // self._clip_len = content.len();
                // self._clip = content[..15].to_string();
            }
            JerryMessage::Request(_) => {}
            JerryMessage::Handshake(_echo, _) => {}
            JerryMessage::Heartbeat => self.heart = 3,
        }
    }

    fn get_color(&self) -> Color {
        match (&self.connection_state, self.active) {
            (_, true) => Color::Green,
            (ConnectionState::HandshakeSuccess(_), _) => Color::LightBlue,
            (ConnectionState::ReadError(_), _) => Color::Red,
            (_, _) => Color::DarkGray,
        }
    }

    fn pause_rendering(&mut self, cycles: u16) {
        self.rendering_pause_cycles = cycles;
    }

    fn render(&mut self) {
        if self.rendering_pause_cycles > 0 {
            self.rendering_pause_cycles -= 1;
            return;
        }
        let active_color = self.get_color();

        match self.terminal.size() {
            Err(_) => return,
            Ok(size) => {
                if size.height < 16 || size.width < 80 {
                    // small terminal size
                    _ = self.terminal.draw(|f| {
                        f.render_widget(
                            Block::default()
                                .borders(Borders::NONE)
                                .title(Title::from("Error: Terminal size is too small.")),
                            f.size(),
                        )
                    });
                    return;
                }
            }
        }

        _ = self.terminal.draw(|f| {
            let chunks = Layout::default()
                .direction(Direction::Vertical)
                .constraints([Constraint::Min(8), Constraint::Length(5)].as_ref())
                .split(f.size());

            let canvas_monitor = Canvas::default()
                .marker(symbols::Marker::Braille)
                .block(
                    Block::default()
                        .borders(Borders::RIGHT | Borders::LEFT)
                        .border_type(BorderType::Thick)
                        .border_style(Style::default())
                        .style(Style::default().fg(active_color)),
                )
                .paint(|ctx| {
                    ctx.print(
                        self.cursor.x as f64,
                        (-self.cursor.y) as f64,
                        Span::styled("O", Style::default().fg(active_color)),
                    );
                })
                .x_bounds([0.0, (self.mon_size.x) as f64])
                .y_bounds([(-self.mon_size.y) as f64, 0.0]);

            let (style, char_heart) = if self.heart > 0 {
                self.heart -= 1;
                if self.heart.is_even() {
                    (Style::default().fg(Color::Red), "❤")
                } else {
                    (Style::default().fg(Color::Red), " ")
                }
            } else {
                (Style::default().fg(Color::Red), " ♥ ")
            };

            let canvas_btn = Canvas::default()
                .marker(symbols::Marker::Braille)
                .block(
                    Block::default()
                        .borders(Borders::ALL)
                        .border_type(BorderType::Double)
                        .border_style(Style::default())
                        .style(Style::default().fg(active_color)),
                )
                .paint(|ctx| {
                    let mut mouse_state = self.mouse_btn.to_string();
                    mouse_state.push(self.wheel);

                    ctx.print(
                        0.0,
                        0.0,
                        Span::styled(
                            char_heart.to_string(),
                            style,
                            // format!(
                            //    {} {} {}",
                            //  self.mouse_btn, self.wheel, self.wheel_n
                            // ),
                            // style,
                        ),
                    );

                    ctx.print(
                        3.0,
                        0.0,
                        Span::styled(
                            format!("Connection: {}", self.connection_state),
                            Style::default().fg(active_color),
                        ),
                    );

                    ctx.print(
                        0.0,
                        1.0,
                        Span::styled(
                            format!("Wheel: {} Buttons: {}", self.wheel, self.mouse_btn),
                            Style::default().fg(active_color),
                        ),
                    );

                    let stre = format!(
                        "Keys pressed: {}",
                        self.keys
                            .iter()
                            .map(|i| format!("{:?}", crate::emulation::JKey::from(*i as u8)))
                            .collect::<Vec<_>>()
                            .join(", ")
                    );

                    ctx.print(
                        0.0,
                        3.0,
                        Span::styled(stre, Style::default().fg(active_color)),
                    );
                })
                .x_bounds([0.0, 70.0])
                .y_bounds([0.0, 5.0]);

            f.render_widget(canvas_monitor, chunks[0]);
            f.render_widget(canvas_btn, chunks[1]);

            //f.render_stateful_widget(widget, area, state)
        });
    }
}
