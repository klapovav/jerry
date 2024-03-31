use super::proto_factory::{request, response};
use crate::core::{JerryMessage, JerryResponse};
use crate::proto_rs::{proto_in, proto_out};
use crate::proto_rs::{Clip_Format, Clipboard};
use crate::proto_rs::{ProtoInMsg, ProtoOutMsg};
use proto_in::MasterMessage_oneof_action as MsgType;
use std::convert::From;

impl From<ProtoInMsg> for JerryMessage {
    fn from(msg: ProtoInMsg) -> Self {
        if let Some(action) = msg.action {
            match action {
                MsgType::mouse_position(p) => p.into(),
                MsgType::keyboard(k) => k.into(),
                MsgType::mouse_click(mc) => mc.into(),
                MsgType::clipboard(c) => c.into(),
                MsgType::mouse_wheel(wh) => wh.into(),
                MsgType::handshake(result) => result.into(),
                MsgType::start_session(s) => s.into(),
                MsgType::end_session(_) => JerryMessage::SessionEnd,
                MsgType::request(req) => JerryMessage::Request(req),
                MsgType::heartbeat(_one_way) => JerryMessage::Heartbeat,
            }
        } else {
            JerryMessage::Heartbeat
        }
    }
}

impl From<JerryMessage> for ProtoInMsg {
    fn from(params: JerryMessage) -> Self {
        let mut msg = ProtoInMsg::new();
        match params {
            JerryMessage::MouseMove(x, y) => {
                msg.set_mouse_position(request::create_mouse_move(x, y))
            }
            JerryMessage::Key(key, motion) => msg.set_keyboard(request::create_key(key, motion)),
            JerryMessage::MouseClick(b, mo) => {
                msg.set_mouse_click(request::create_mouse_click(b, mo))
            }
            JerryMessage::SessionBegin { relative_move: b } => {
                msg.set_start_session(request::create_session_begin(b))
            }
            JerryMessage::SessionEnd => msg.set_end_session(proto_in::SessionEnd::new()),
            JerryMessage::Clipboard(content, files) => {
                msg.set_clipboard(response::create_clipboard(content, files))
            }
            JerryMessage::Request(r) => msg.set_request(r),
            JerryMessage::MouseWheel(wh, am) => {
                msg.set_mouse_wheel(request::create_mouse_wheel(wh, am))
            }
            JerryMessage::Handshake(res, m) => {
                let mut r = proto_in::Echo::new();
                r.set_result(res);
                r.set_message(m);

                msg.set_handshake(r)
            }
            JerryMessage::Heartbeat => {
                let mut hb = proto_in::Heartbeat::new();
                hb.set_one_way(true);
                msg.set_heartbeat(hb)
            }
        }
        msg
    }
}

//                                      ┌─────────────────────┐
//                                      │    Slave / Client   │
//                                      ├─────────────────────┤
//                                      │                     │
//                  ProtoIn             │    JerryMessage     │
//            ┌─┬─┬─┬─┬─┬─┬─┬─┬─┐       │          │          │
//            └─┴─┴─┴─┴─┴─┴─┴─┴─┘       │          │          │
//          ───────────────────────►    │   ┌──────▼─────┐    │
//          ◄───────────────────────    │   │ MsgHandler │    │
//            ┌─┬─┬─┬─┬─┬─┬─┬─┬─┐       │   └──────┬─────┘    │
//            └─┴─┴─┴─┴─┴─┴─┴─┴─┘       │          │          │
//                  ProtoOut  ◄─────────┼─   (JerryResponse)  │
//                                      └─────────────────────┘

impl From<JerryResponse> for ProtoOutMsg {
    fn from(params: JerryResponse) -> Self {
        let mut msg = ProtoOutMsg::new();
        match params {
            JerryResponse::Cursor(x, y) => msg.set_cursor(response::create_position(x, y)),
            JerryResponse::InitInfo(params) => {
                msg.set_init_info(response::create_init_info(params))
            }
            JerryResponse::Clipboard(content, files) => {
                msg.set_clipboard_session(response::create_clipboard(content, files))
            }
            JerryResponse::NoResponse(reason) => {
                msg.set_no_response(response::create_failure(reason))
            }
        }
        msg
    }
}

impl From<proto_out::Position> for ProtoOutMsg {
    fn from(pos: proto_out::Position) -> Self {
        let mut msg = ProtoOutMsg::new();
        msg.set_cursor(pos);
        msg
    }
}

impl From<Clipboard> for ProtoOutMsg {
    fn from(clip: Clipboard) -> Self {
        let mut msg = ProtoOutMsg::new();
        msg.set_clipboard_session(clip);
        msg
    }
}

impl From<proto_out::ClientInfo> for ProtoOutMsg {
    fn from(init: proto_out::ClientInfo) -> Self {
        let mut msg = ProtoOutMsg::new();
        msg.set_init_info(init);
        msg
    }
}
impl From<proto_out::Failure> for ProtoOutMsg {
    fn from(fail: proto_out::Failure) -> Self {
        let mut msg = ProtoOutMsg::new();
        msg.set_no_response(fail);
        msg
    }
}

pub trait GetSlaveMessage {
    fn into_response(self) -> ProtoOutMsg;
}

impl GetSlaveMessage for proto_out::Position {
    fn into_response(self) -> ProtoOutMsg {
        let mut msg = ProtoOutMsg::new();
        msg.set_cursor(self);
        msg
    }
}

impl GetSlaveMessage for Clipboard {
    fn into_response(self) -> ProtoOutMsg {
        let mut msg = ProtoOutMsg::new();
        msg.set_clipboard_session(self);
        msg
    }
}

impl GetSlaveMessage for proto_out::ClientInfo {
    fn into_response(self) -> ProtoOutMsg {
        let mut msg = ProtoOutMsg::new();
        msg.set_init_info(self);
        msg
    }
}

//============================
//   proto_in -> Jerry_Message
//============================

impl From<proto_in::MouseMove> for JerryMessage {
    fn from(mm: proto_in::MouseMove) -> Self {
        JerryMessage::MouseMove(mm.X, mm.Y)
    }
}
impl From<proto_in::Keyboard> for JerryMessage {
    fn from(ke: proto_in::Keyboard) -> Self {
        JerryMessage::Key(ke.key, ke.event_type)
    }
}
impl From<Clipboard> for JerryMessage {
    fn from(c: Clipboard) -> Self {
        JerryMessage::Clipboard(c.message, c.format == Clip_Format::FILE)
    }
}
impl From<proto_in::MouseClick> for JerryMessage {
    fn from(mc: proto_in::MouseClick) -> Self {
        JerryMessage::MouseClick(mc.button, mc.event_type)
    }
}
impl From<proto_in::MouseWheel> for JerryMessage {
    fn from(wh: proto_in::MouseWheel) -> Self {
        JerryMessage::MouseWheel(wh.scroll_direction, wh.amount)
    }
}
impl From<proto_in::Echo> for JerryMessage {
    fn from(result: proto_in::Echo) -> Self {
        JerryMessage::Handshake(result.result, result.message)
    }
}
impl From<proto_in::SessionBegin> for JerryMessage {
    fn from(s: proto_in::SessionBegin) -> Self {
        JerryMessage::SessionBegin {
            relative_move: s.mouse_move_relative,
        }
    }
}
