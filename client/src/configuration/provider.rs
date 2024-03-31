use eyre::Context;
use eyre::{eyre, Result};
use std::fs::File;
use std::io::prelude::*;
use std::net::Ipv4Addr;
use sysinfo::{System, SystemExt};
use tracing::info;
use uuid::Uuid;

pub struct ConfigProvider {
    config: Config,
    config_path: String,
    hint: String,
}
impl ConfigProvider {
    pub fn new(path: &str) -> Result<ConfigProvider> {
        let configuration = match Self::read_from_file(path) {
            Ok(file_content) => Self::deserialize(&file_content)
                .wrap_err(format!("Configuration file {} is not valid toml!", path)),
            Err(_) => Ok(Self::create_default_config()),
        }?;
        configuration
            .validate()
            .wrap_err("Configuration file is not valid!")?;

        Ok(Self {
            config: configuration,
            config_path: String::from(path),
            hint: String::from(
                "
                #----------------------
                # [[servers]]           
                # name       : required    must be unique
                # ip         : required    
                # port       : required    
                # password   : required
                #----------------------",
            ),
        })
    }

    pub fn get_default_port(&self) -> u16 {
        self.config.connection.port
    }
    pub fn get_init_info_for_last_server(&self) -> Option<ClientInfo> {
        match self.get_server(self.config.connection.previous.as_ref().unwrap()) {
            None => None,
            Some(server) => {
                let mut info = self.config.client.clone();
                if let Some(guid) = server.guid.as_ref() {
                    info.default_guid = guid.clone();
                }
                Some(info)
            }
        }
    }
    pub fn get_last_connection_info(&self) -> Option<ServerConfig> {
        if let Some(ls) = self.config.connection.previous.as_ref() {
            match self.get_server(ls) {
                Some(s) => {
                    let mut s_clone = s.clone();
                    if s_clone.guid.is_none() {
                        s_clone.guid = Some(String::from(&self.config.client.default_guid));
                    }
                    Some(s_clone)
                }
                None => None,
            }
        } else {
            None
        }
    }
    pub fn get_server(&self, name: &str) -> Option<&ServerConfig> {
        match self.config.servers.as_ref() {
            Some(servers) => {
                for s in servers {
                    if s.name == name {
                        return Some(s);
                    }
                }
                None
            }
            None => None,
        }
    }

    pub fn get_servers(&self) -> Vec<String> {
        match &self.config.servers {
            None => Vec::new(),
            Some(servers_c) => servers_c.iter().map(|s| s.name.clone()).collect(),
        }
    }
    pub fn save_configuration(&self) -> Result<()> {
        //} std::io::Result<()> {
        let mut file = File::create(self.config_path.as_str())?;
        let toml_str = self.serialize()?;
        file.write_all(toml_str.as_bytes())?;
        Ok(())
    }
    pub fn connect_without_confirmation(&self) -> bool {
        !self.config.connection.confirm
    }
    pub fn update_last(&mut self, name: &str) {
        self.config.connection.previous = Some(String::from(name));
    }

    pub fn add_server(&mut self, server: &ServerConfig) -> Result<()> {
        self.add_new_server(server.ip, server.port, &server.name, &server.password)?;
        self.update_server(&server.name, server.guid.as_deref())?;
        Ok(())
    }

    fn add_new_server(
        &mut self,
        ip: Ipv4Addr,
        port: u16,
        name: &str,
        password: &str,
    ) -> Result<()> {
        if self.config.servers.is_none() {
            self.config.servers = Some(Vec::new());
        }
        self.check_if_unique_ex(name)?;
        //ok unwrap
        self.config.servers.as_mut().unwrap().push(ServerConfig {
            name: String::from(name),
            ip,
            port,
            password: String::from(password),
            guid: None,
        });

        self.config.connection.previous = Some(String::from(name));
        Ok(())
    }

    pub fn update_server(&mut self, name: &str, guid: Option<&str>) -> Result<()> {
        match self.config.servers.as_mut() {
            Some(servers) => {
                for s in servers {
                    if s.name == name {
                        if let Some(new_value) = guid {
                            s.guid = Some(String::from(new_value));
                        }
                        return Ok(());
                    }
                }
                Err(eyre!(
                    "There is no server with specified name '{}' in the configuration",
                    name
                ))
            }
            None => Err(eyre!("There are no server in the configuration")),
        }
    }

    fn check_if_unique_ex(&self, name: &str) -> Result<()> {
        if self.config.servers.is_none() {
            return Ok(());
        }
        if self
            .config
            .servers
            .as_ref()
            .unwrap() //checked
            .iter()
            .any(|s| s.name == name)
        {
            return Err(eyre!(
                "There is already a server with name '{}' in the configuration",
                name
            ));
        }
        Ok(())
    }

    fn read_from_file(path: &str) -> Result<String, std::io::Error> {
        let mut file = File::open(path)?;
        let mut contents = String::new();
        file.read_to_string(&mut contents)?;
        Ok(contents)
    }
    fn deserialize(s: &str) -> Result<Config, toml::de::Error> {
        let r = toml::from_str(s);
        match r {
            Ok(result) => Ok(result),
            Err(e) => Err(e),
        }
    }
    fn serialize(&self) -> Result<String> {
        let s = toml::to_string_pretty(&self.config)?;
        let ss = s + &self.hint;
        Ok(ss)
    }

    fn create_default_config() -> Config {
        info!("Generating default configuration file");
        let mut sys = System::new_all();
        sys.refresh_all();

        let ss = vec![];

        let random_id = Uuid::new_v4();
        Config {
            client: ClientInfo {
                name: sys.host_name().expect("computer_name"),
                default_guid: random_id.to_string(),
            },
            connection: Connection {
                port: crate::DEFAULT_PORT,
                previous: None,
                confirm: true,
            },
            servers: Some(ss),
        }
    }
}

use serde_derive::Deserialize;
use serde_derive::Serialize;
use validator::Validate;

#[derive(Serialize, Validate, Deserialize, Debug)]
pub struct Config {
    #[validate]
    client: ClientInfo,
    #[validate]
    connection: Connection,
    #[validate]
    servers: Option<Vec<ServerConfig>>,
}

#[derive(Serialize, Deserialize, Validate, Debug, Clone)]
pub struct ClientInfo {
    #[validate(length(min = 1))]
    pub name: String,
    pub default_guid: String,
}
#[derive(Serialize, Validate, Deserialize, Debug)]
pub struct Connection {
    //1024..49151 registered
    //49152..65535 dynamic
    #[validate(range(min = 1024, max = 49151))]
    port: u16,
    previous: Option<String>,
    confirm: bool,
}

#[derive(Serialize, Validate, Deserialize, Debug, Clone)]
pub struct ServerConfig {
    #[validate(length(min = 1))]
    pub name: String,
    pub ip: Ipv4Addr,
    #[validate(range(min = 1024, max = 65535))]
    pub port: u16,
    #[validate(length(min = 4))]
    pub password: String,
    //#[validate(Uuid)]
    //#[validate(custom = "validate_guid")]
    pub guid: Option<String>,
}

// fn validate_guid(input: &str) -> Result<(), ValidationError> {
//     if input == String::from("xXx") {
//         return Err(ValidationError::new("Invalid UUid"));
//     }
//     Ok(())
// }
