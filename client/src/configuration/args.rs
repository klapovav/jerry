use crate::DEFAULT_PORT;
use clap::{Args, Parser, Subcommand};
use uuid::Uuid;

#[derive(Subcommand, Clone, Debug)]
pub enum Commands {
    Localhost(LocalhostArgs),
}
#[derive(Args, Clone, Debug)]
pub struct LocalhostArgs {
    /// Width of monitor (in pixels)
    pub width: u16,
    /// Height  of monitor (in pixels)
    pub height: u16,
    /// Identifier of mock computer
    pub guid: Uuid,
    /// Password
    #[arg(default_value = "Test")]
    /// Name of mock computer
    pub name: String,
    #[arg(default_value_t = DEFAULT_PORT)]
    pub port: u16,
    #[arg(default_value = "2002")]
    pub password: String,
}

#[derive(Parser, Clone, Debug)]
#[command(version, about, long_about = "")]
pub struct Cli {
    #[command(subcommand)]
    pub command: Option<Commands>,
    #[arg(long, short, default_value_t = false)]
    pub visualizer: bool,
    #[arg(long, short, default_value_t = false)]
    pub emulate: bool,
}
