use crate::proto_rs::{Clip_Format, Clipboard};
pub fn create_clipboard(content: String, filelist: bool) -> Clipboard {
    let mut data = Clipboard::new();
    if filelist {
        data.set_format(Clip_Format::FILE)
    }
    data.set_message(content);
    data
}

//========================================
//           proto_out::*
//========================================
pub mod response {
    use crate::{
        configuration::{ScreenResolution, SessionParams},
        proto_rs::proto_out,
    };
    use enigo::{Enigo, MouseControllable};

    pub fn create_position(x: i32, y: i32) -> proto_out::Position {
        let mut pos = proto_out::Position::new();
        pos.set_x(x);
        pos.set_y(y);
        pos
    }

    pub fn create_clipboard(content: String, filelist: bool) -> super::Clipboard {
        super::create_clipboard(content, filelist)
    }
    pub fn create_failure(reason: String) -> proto_out::Failure {
        let mut f = proto_out::Failure::new();
        f.set_Reason(reason);
        f
    }

    pub fn create_init_info(session: SessionParams) -> proto_out::ClientInfo {
        tracing::debug!("{:?}", session);
        let (cx, cy): (i32, i32) = match session.monitor {
            ScreenResolution::Static(fix_size) => {
                (fix_size.width as i32 / 2, fix_size.height as i32 / 2)
            }
            ScreenResolution::Dynamic => Enigo::new().mouse_location(),
        };
        let (w, h) = match session.monitor {
            ScreenResolution::Static(fix_size) => (fix_size.width as i32, fix_size.height as i32),
            ScreenResolution::Dynamic => Enigo::new().main_display_size(),
        };

        let mut guid = proto_out::ClientInfo_UUID::new();
        guid.set_value(session.client_guid);

        let mut info = proto_out::ClientInfo::new();
        info.set_Width(w);
        info.set_Height(h);
        info.set_Cursor(create_position(cx, cy));
        info.set_Password(session.server_password);
        info.set_Guid(guid);
        info.set_Name(session.client_name);
        info.set_System(get_os());
        info
    }
    fn get_os() -> proto_out::ClientInfo_OS {
        if cfg!(windows) {
            proto_out::ClientInfo_OS::WINDOWS
        } else if cfg!(macos) {
            proto_out::ClientInfo_OS::MAC
        } else {
            proto_out::ClientInfo_OS::LINUX
        }
    }
}

//========================================
//           proto_in::*
//========================================
#[allow(dead_code)]
pub mod request {
    use crate::proto_rs::proto_in;
    use crate::proto_rs::proto_in::Direction;

    pub fn create_mouse_move(x: i32, y: i32) -> proto_in::MouseMove {
        let mut mm = proto_in::MouseMove::new();
        mm.set_X(x);
        mm.set_Y(y);
        mm
    }

    pub fn create_mouse_click(
        b: proto_in::Button,
        motion: proto_in::State,
    ) -> proto_in::MouseClick {
        let mut mm = proto_in::MouseClick::new();
        mm.set_button(b);
        mm.set_event_type(motion);
        mm
    }
    pub fn create_mouse_wheel(wh: Direction, amount: i32) -> proto_in::MouseWheel {
        let mut wheel = proto_in::MouseWheel::new();
        wheel.set_scroll_direction(wh);
        wheel.set_amount(amount);
        wheel
    }
    pub fn create_key(key: u32, motion: proto_in::State) -> proto_in::Keyboard {
        let mut km = proto_in::Keyboard::new();
        km.set_key(key);
        km.set_event_type(motion);
        km
    }
    pub fn create_session_begin(mouse_move_relative: bool) -> proto_in::SessionBegin {
        let mut sb = proto_in::SessionBegin::new();
        sb.set_mouse_move_relative(mouse_move_relative);
        sb
    }
    pub fn create_clipboard(content: String, filelist: bool) -> super::Clipboard {
        super::create_clipboard(content, filelist)
    }
}

//========================================
