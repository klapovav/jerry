pub mod mapper;
pub mod proto_factory;
use crate::core::JerryMessage;
use crate::core::MessageConsumer;
use crate::proto_rs::{ProtoInMsg, ProtoOutMsg};

use eyre::{eyre, Result};
use protobuf::ProtobufResult;
use std::io::{Read, Write};

pub struct ProtoSerDe<'a> {
    pub stream_in: protobuf::CodedInputStream<'a>,
    pub stream_out: protobuf::CodedOutputStream<'a>,
}

impl<'a> ProtoSerDe<'a> {
    pub fn new(stream_in: &'a mut dyn Read, stream_out: &'a mut dyn Write) -> Self {
        let stream_in = protobuf::CodedInputStream::new(stream_in);
        let stream_out = protobuf::CodedOutputStream::new(stream_out);
        ProtoSerDe {
            stream_in,
            stream_out,
        }
    }

    pub fn listen_loop(&mut self, mut consumer: Box<dyn MessageConsumer>) -> Result<()> {
        loop {
            if consumer.finished() {
                return Ok(());
            }
            let _msg_in = self
                .stream_in
                .read_message::<ProtoInMsg>()
                .map(JerryMessage::from)
                .map_err(|e| eyre!("Read message error: {:?}", e))?;
            consumer
                .consume(_msg_in)
                .map(ProtoOutMsg::from)
                .map(|r| self.write_flush(r))
                .unwrap_or(Ok(()))
                .map_err(|e| eyre!("Write message error: {:?}", e))?;
        }
    }

    fn write_flush(&mut self, msg: ProtoOutMsg) -> ProtobufResult<()> {
        self.stream_out.write_message_no_tag(&msg)?;
        self.stream_out.flush()
    }
}
