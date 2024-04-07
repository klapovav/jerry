pub mod args;
mod provider;
use self::provider::ServerConfig;
use crate::{configuration, DisplayMode, CONFIGURATION_FILE};
pub use args::Cli;
use dialoguer::{console::Term, theme::ColorfulTheme, Input, Select};
pub use provider::ConfigProvider;
use std::{io::ErrorKind, net::Ipv4Addr, str::FromStr};
use tracing::{self, error, info};

pub fn get_session_info_localhost(args: &args::LocalhostArgs) -> SessionParams {
    SessionParams {
        client_name: args.name.clone(),
        client_guid: args.guid.to_string(),
        port: args.port,
        monitor: ScreenResolution::Static(MonitorSize {
            width: args.width,
            height: args.height,
        }),
        server_password: args.password.clone(),

        ip: Ipv4Addr::LOCALHOST,
        display_mode: DisplayMode::CurrentState,
        emulate_events: false,
    }
}

pub fn get_session_info(cli: Cli) -> Option<SessionParams> {
    let mut provider = match configuration::ConfigProvider::new(CONFIGURATION_FILE) {
        Ok(cp) => cp,
        Err(_e) => {
            error!(name: "Config provider constructor", "Configuration: {}", _e);
            return None;
        }
    };

    if !provider.connect_without_confirmation() {
        if let Err(_e) = crate::configuration::update_configuration_using_prompt(&mut provider) {
            // "User did not select a server to connect to;
            return None;
        }

        provider
            .save_configuration()
            .expect("Error saving configuration");
    }

    let client = provider.get_init_info_for_last_server().unwrap();

    let server_specific = provider
        .get_last_connection_info()
        .expect("Set last connection in toml");

    println!(
        "\n========================\n Connecting to server : \n{}\n========================\n",
        toml::to_string_pretty(&server_specific).expect("Serialization failed")
    );

    let display_mode = match cli.visualizer {
        true => DisplayMode::CurrentState,
        false => DisplayMode::Logging,
    };

    Some(SessionParams {
        client_name: client.name,
        client_guid: server_specific.guid.unwrap_or(client.default_guid),
        monitor: ScreenResolution::Dynamic,

        server_password: server_specific.password,
        ip: server_specific.ip,
        port: server_specific.port,

        emulate_events: !server_specific.ip.is_loopback() | cli.emulate,
        display_mode,
    })
}

pub fn update_configuration_using_prompt(cp: &mut ConfigProvider) -> std::io::Result<()> {
    let servers = cp.get_servers();

    let last = cp
        .get_last_connection_info()
        .map_or(String::new(), |l| l.name);

    let last_index = servers.iter().position(|s| s.eq(&last)).unwrap_or(0);

    let selection = Select::with_theme(&ColorfulTheme::default())
        .with_prompt("Select a server and press [ENTER]. Press [ESC]/[Q] to exit.")
        .default(last_index)
        .items(&servers[..])
        .item("[*] Create new record")
        //.items(&selections[..])
        .interact_on_opt(&Term::stderr())?;
    match selection {
        Some(index) if index == servers.len() => {
            //Add new Server
            let new_server = prompt_new_server(cp);
            if cp.add_server(&new_server).is_err() {
                return Err(std::io::Error::new(ErrorKind::InvalidInput, "XXX"));
            }
            cp.update_last(&new_server.name);
            // server_conf = cp.get_server(&new_server.name).unwrap();
        }
        Some(selected_item) => cp.update_last(&servers[selected_item]),
        None => {
            return Err(std::io::Error::new(
                ErrorKind::Interrupted,
                "The key [ESC]/[Q] was pressed",
            ))
        }
    }
    Result::Ok(())
}

fn prompt_new_server(cp: &ConfigProvider) -> ServerConfig {
    let name = prompt_name(cp);
    let ip = prompt_ipv4(false);
    let port = Input::<u16>::with_theme(&ColorfulTheme::default())
        .with_prompt("Port:\n")
        .with_initial_text(cp.get_default_port().to_string())
        .interact_text()
        .unwrap();
    let password = Input::with_theme(&ColorfulTheme::default())
        .with_prompt("Password:\n")
        .interact_text()
        .unwrap();
    ServerConfig {
        name,
        ip,
        port,
        password,
        guid: None,
    }
}

fn prompt_ipv4(recursive: bool) -> Ipv4Addr {
    let prompt_val = if recursive {
        String::from("IP address, e.g. 192.168.1.66\n")
    } else {
        String::from("IP address:\n")
    };
    let ip_res = prompt_ip_par(prompt_val);
    match ip_res {
        Some(ip) => ip,
        None => prompt_ipv4(true),
    }
}
fn prompt_ip_par(prompt: String) -> Option<Ipv4Addr> {
    let a = Input::<String>::with_theme(&ColorfulTheme::default())
        .with_prompt(prompt)
        .validate_with({
            move |input: &String| -> Result<(), String> {
                match Ipv4Addr::from_str(input) {
                    Ok(_) => Ok(()),
                    Err(e) => Err(e.to_string()),
                }
            }
        })
        .interact_text();
    a.map(|ip| Ipv4Addr::from_str(&ip).unwrap()).ok()
}

fn prompt_name(cp: &ConfigProvider) -> String {
    let name_res = Input::with_theme(&ColorfulTheme::default())
        .with_prompt("Name:\n")
        .validate_with({
            let servers_used = cp.get_servers().join(", ");
            let error = std::format!(
                "Input error: server name must be unique. The following values are already in use: {:?}.",
                servers_used
            );
            move |input: &String| -> Result<(), String> {
                match cp.get_server(input) {
                    None => Ok(()),
                    Some(_) => Err(error.clone()),
                }
            }
        })
        .interact_text();
    match name_res {
        Err(_) => prompt_name(cp),
        Ok(name) => name,
    }
}
#[derive(Clone, Debug)]
pub struct SessionParams {
    pub client_name: String,
    pub client_guid: String,
    pub server_password: String,
    pub monitor: ScreenResolution,
    pub ip: Ipv4Addr,
    pub port: u16,
    pub display_mode: DisplayMode,
    pub emulate_events: bool,
}

#[derive(Clone, Copy, Debug)]
pub struct MonitorSize {
    pub width: u16,
    pub height: u16,
}
#[derive(Clone, Copy, Debug)]
pub enum ScreenResolution {
    Static(MonitorSize),
    Dynamic,
}
