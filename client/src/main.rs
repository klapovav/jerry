mod configuration;
mod connection;
mod core;
mod emulation;
mod proto_rs;
mod security;
mod serialization;
mod state;

use crate::core::Command;
use clap::Parser;
use configuration::{ScreenResolution, SessionParams};
use connection::ConnectionWorker;
use std::sync::mpsc::{self, Receiver, Sender};
use std::thread::JoinHandle;
use tracing::{error, info, trace, Level};

const ENCRYPT: bool = false;
const DEFAULT_PORT: u16 = 8888;
const JERRY_CLIENT_ID: usize = 23889;
const CONFIGURATION_FILE: &str = "jerry_client.toml";
const LOG_LEVEL_FILE: Level = Level::INFO;
const LOG_LEVEL_STD: Level = Level::INFO;

#[derive(Debug, Clone, Copy)]
pub enum DisplayMode {
    Logging,
    CurrentState,
}
fn main() -> eyre::Result<()> {
    std::env::set_var("RUST_BACKTRACE", "1");

    use configuration::args::{Cli, Commands};
    let cli = Cli::parse();
    let c_info = match &cli.command {
        None => match configuration::get_session_info(cli.clone()) {
            Some(e) => e,
            None => return Ok(()), // Q/ESC key -> Exit
        },
        Some(Commands::Localhost(_args)) => configuration::get_session_info_localhost(_args),
    };

    let display_mode = match c_info.visualize {
        true => DisplayMode::CurrentState,
        false => DisplayMode::Logging,
    };

    let _guards = logger_init(display_mode, LOG_LEVEL_FILE, LOG_LEVEL_STD);
    info!("Program start");

    type Channel = (Sender<Command>, Receiver<Command>);
    let (tx, rx): Channel = mpsc::channel();
    let key_listener = start_exit_key_listener(tx.clone());
    let ui_thread = start_state_visualization(tx.clone(), c_info.clone(), display_mode, rx);
    let conn_worker = start_connection_loop(tx, c_info);

    if let Err(e) = ui_thread.join() {
        error!("View thread panicked: {:?}", e.downcast_ref::<&str>())
    }
    trace!("Exiting the program: 1/4 |  View thread has finished execution.");
    if let Err(e) = key_listener.join() {
        error!("KeyListener panicked: {:?}", e.downcast_ref::<&str>())
    }
    trace!("Exiting the program: 2/4 |  KeyListener thread has finished execution.");
    if let Err(e) = conn_worker.join() {
        error!("Connection thread panicked: {:?}", e.downcast_ref::<&str>())
    }
    trace!(
        "Exiting the program: 3/4 |  Connection & MessageHandler thread has finished execution."
    );
    trace!("Exiting the program: 4/4 |  Main thread has finished execution. ");
    std::thread::sleep(std::time::Duration::from_millis(300));
    Ok(())
}

use tracing_appender::non_blocking::WorkerGuard;
fn logger_init(strategy: DisplayMode, file_level: Level, out_level: Level) -> Vec<WorkerGuard> {
    use tracing::level_filters::LevelFilter;
    use tracing_subscriber::{
        fmt::{self},
        prelude::*,
        Registry,
    };

    let (nb_console_appender, guard_console) = tracing_appender::non_blocking(std::io::stdout());
    let file_appender = tracing_appender::rolling::daily("log", "dbg");
    let (nb_file_appender, guard_file) = tracing_appender::non_blocking(file_appender);

    // let fmt = format().with_timer(time::Uptime::default());
    let layer_file = fmt::Layer::default()
        //.event_format(fmt)
        .with_ansi(false)
        .with_writer(nb_file_appender)
        .with_filter(LevelFilter::from(file_level));

    let layer_console = fmt::Layer::default()
        .with_writer(nb_console_appender)
        .with_filter(LevelFilter::from(out_level));

    match strategy {
        DisplayMode::Logging => {
            Registry::default()
                .with(layer_file)
                .with(layer_console)
                .init();
            vec![guard_console, guard_file]
        }
        DisplayMode::CurrentState => {
            Registry::default().with(layer_file).init();
            vec![guard_file]
        }
    }
}

fn start_state_visualization(
    tx: Sender<Command>,
    info: SessionParams,
    mode: DisplayMode,
    rx: Receiver<Command>,
) -> JoinHandle<()> {
    std::thread::spawn(move || match mode {
        DisplayMode::CurrentState => view_thread_job(info, tx, rx),
        DisplayMode::Logging => log_thread_job(rx),
    })
}

fn start_connection_loop(transmitter: Sender<Command>, cinfo: SessionParams) -> JoinHandle<()> {
    std::thread::spawn(move || {
        use thread_priority::*;
        _ = set_current_thread_priority(ThreadPriority::Max);
        let mut conw = ConnectionWorker::new(transmitter, cinfo);
        conw.run();
    })
}

fn start_exit_key_listener(exit_transmitter: Sender<Command>) -> JoinHandle<()> {
    use crossterm::event::{self, Event, KeyCode};
    std::thread::spawn(move || {
        loop {
            match event::poll(std::time::Duration::from_millis(1000)) {
                Err(_) => {
                    _ = exit_transmitter.send(Command::Halt);
                    break;
                }
                Ok(false) => {
                    if exit_transmitter.send(Command::Draw).is_err() {
                        break;
                    }
                }
                Ok(true) => {
                    //event is available
                    if let Ok(key_ev) = event::read() {
                        if let Event::Key(key) = key_ev {
                            if key.code == KeyCode::Char('q') || key.code == KeyCode::Esc {
                                _ = exit_transmitter.send(Command::Halt);
                                break;
                            }
                        }
                    } else {
                        _ = exit_transmitter.send(Command::Halt);
                        break;
                    }
                }
            }
        }
    })
}

fn view_thread_job(info: SessionParams, tx_clone: Sender<Command>, rx: Receiver<Command>) {
    use crossterm::execute;
    use crossterm::terminal::{
        disable_raw_mode, enable_raw_mode, EnterAlternateScreen, LeaveAlternateScreen,
    };
    use ratatui::{backend::CrosstermBackend, Terminal};
    enable_raw_mode().unwrap();
    let mut stdout = std::io::stdout();
    execute!(stdout, EnterAlternateScreen).unwrap();
    let backend = CrosstermBackend::new(stdout);
    let mut terminal = Terminal::new(backend).unwrap();
    terminal.clear().unwrap();

    let (w, h) = match info.monitor {
        ScreenResolution::Static(size) => (size.width as i32, size.height as i32),
        ScreenResolution::Dynamic => {
            use enigo::{Enigo, MouseControllable};
            Enigo::new().main_display_size()
        }
    };
    let resolution = state::Coord { x: w, y: h };
    let mut view = state::ui::WindowState::new(resolution, tx_clone, rx, &mut terminal);
    view.run();

    disable_raw_mode().unwrap();
    execute!(terminal.backend_mut(), LeaveAlternateScreen).unwrap();
    terminal.show_cursor().unwrap();
}
fn log_thread_job(rx: Receiver<Command>) {
    let mut logging_receiver = state::log::View::new(rx);
    logging_receiver.run();
}
