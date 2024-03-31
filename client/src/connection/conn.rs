use std::io::{Error, ErrorKind};
use std::net::{IpAddr, Ipv4Addr};
use std::net::{SocketAddr, TcpStream};
use std::thread::sleep;
use std::time::{Duration, Instant};
use tap::tap::*;
use tracing::{debug, warn};
pub struct Connection {
    ip: Ipv4Addr,
    port: u16,
    timeout: Duration,
    reconnection_interval: Duration,
    automatic_reconnection: bool,
    on_error: Option<fn(ErrorKind) -> Option<ErrorKind>>,
}

impl Connection {
    pub fn new(ip: Ipv4Addr, port: u16) -> Self {
        let timeout = Duration::MAX;
        let repeated_rest = Duration::from_secs(5);
        Connection {
            ip,
            port,
            timeout,
            reconnection_interval: repeated_rest,
            automatic_reconnection: false,
            on_error: None,
        }
    }
    pub fn set_timeout(&mut self, timeout_sec: u64) -> &mut Self {
        self.timeout = Duration::from_secs(timeout_sec);
        self.reconnection_interval = std::cmp::min(self.timeout, self.reconnection_interval);
        self
    }
    pub fn enable_automatic_reconnection(&mut self, interval: u64) -> &mut Self {
        self.automatic_reconnection = true;
        self.reconnection_interval = Duration::from_secs(interval);
        self.timeout = std::cmp::max(self.timeout, self.reconnection_interval);
        self
    }
    pub fn events_on_error(&mut self, on_err: fn(ErrorKind) -> Option<ErrorKind>) -> &mut Self {
        self.on_error = Some(on_err);
        self
    }

    pub fn connect(&mut self) -> Result<TcpStream, Error> {
        match self.automatic_reconnection {
            true => self._reconnection_loop(),
            false => self._connect(),
        }
    }
    fn _connect(&mut self) -> Result<TcpStream, Error> {
        let soc = SocketAddr::new(IpAddr::V4(self.ip), self.port);
        TcpStream::connect_timeout(&soc, self.timeout).tap_err(|e| warn!("{}", e))
    }
    fn _reconnection_loop(&mut self) -> Result<TcpStream, Error> {
        let soc = SocketAddr::new(IpAddr::V4(self.ip), self.port);
        let start = Instant::now();
        let mut last: Error;

        loop {
            debug!("Server: {}", soc);
            match TcpStream::connect_timeout(&soc, self.timeout) {
                //TcpStream::connect(soc )
                Ok(stream) => {
                    debug!("Connected to {}", soc);
                    return Ok(stream);
                }
                Err(e) => {
                    if let Some(fn_err) = self.on_error {
                        let new_error = fn_err(e.kind());
                        debug!("Connection error {:?}", e);
                        if let Some(er_kind) = new_error {
                            return Err(Error::from(er_kind));
                        }
                    } else {
                        debug!("Connection error {:?}", e);
                    }
                    last = e;

                    sleep(self.reconnection_interval);
                }
            }
            if start.elapsed() > self.timeout {
                break;
            }
        }
        Err(last)
    }
}
