pub mod clipboard;
pub mod request_master;
pub mod response_slave;
pub use clipboard::Clipboard;
pub use clipboard::Clipboard_Format as Clip_Format;
pub use request_master as proto_in;
pub use request_master::MasterMessage as ProtoInMsg;
pub use response_slave as proto_out;
pub use response_slave::SlaveMessage as ProtoOutMsg;
