use core::mem::size_of;
use eyre::{eyre, Result};
use tracing::debug;
use windows::Win32::Foundation::POINT;
use windows::Win32::UI::Input::KeyboardAndMouse::{
    SendInput, INPUT, INPUT_0, INPUT_MOUSE, MOUSEEVENTF_ABSOLUTE, MOUSEEVENTF_HWHEEL,
    MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP,
    MOUSEEVENTF_MOVE, MOUSEEVENTF_MOVE_NOCOALESCE, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP,
    MOUSEEVENTF_VIRTUALDESK, MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, MOUSEINPUT,
    MOUSE_EVENT_FLAGS,
};
use windows::Win32::UI::WindowsAndMessaging::{
    //GetMessageExtraInfo, SetMessageExtraInfo
    GetCursorPos,
    GetSystemMetrics,
    SetCursorPos,
    SM_CXSCREEN,
    SM_CXVIRTUALSCREEN,
    SM_CYSCREEN,
    SM_CYVIRTUALSCREEN,
};

use super::Button;
use crate::JERRY_CLIENT_ID;
pub struct Mouse {}

impl Mouse {
    pub fn new() -> Self {
        //let _prev_info = unsafe { SetMessageExtraInfo(LPARAM(JERRY_CLIENT_ID)) };

        Mouse {}
    }
    pub fn press(&mut self, btn: Button) -> Result<()> {
        match btn {
            Button::Left => sim_mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0),
            Button::Middle => sim_mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0),
            Button::Right => sim_mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0),
            Button::Back => sim_mouse_event(MOUSEEVENTF_XDOWN, 1, 0, 0),
            Button::Forward => sim_mouse_event(MOUSEEVENTF_XDOWN, 2, 0, 0),
        }
    }
    pub fn release(&mut self, btn: Button) -> Result<()> {
        match btn {
            Button::Left => sim_mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0),
            Button::Middle => sim_mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0),
            Button::Right => sim_mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0),
            Button::Back => sim_mouse_event(MOUSEEVENTF_XUP, 1, 0, 0),
            Button::Forward => sim_mouse_event(MOUSEEVENTF_XUP, 2, 0, 0),
        }
    }
    pub fn wheel(&mut self, amount: f32, horizontal: bool) -> Result<()> {
        //const WHEEL_DELTA: f32 = 120.0;
        //let data = (amount * WHEEL_DELTA) as i32;
        let data = amount as i32;
        match horizontal {
            true => debug!("simulate horizontal wheel event {:?}", data),
            false => debug!("simulate vertical wheel event {:?}", data),
        }
        match horizontal {
            false => sim_mouse_event(MOUSEEVENTF_WHEEL, data, 0, 0),
            true => sim_mouse_event(MOUSEEVENTF_HWHEEL, data / 2, 0, 0),
        }
    }
}

#[allow(dead_code)]
pub fn mouse_location() -> Option<POINT> {
    let mut point = POINT { x: 666, y: 666 };
    let result = unsafe { GetCursorPos(&mut point) };
    match result.as_bool() {
        true => Some(point),
        false => None,
    }
}
#[allow(dead_code)]
pub fn get_cursor_pos() -> Result<POINT> {
    let mut point = POINT { x: 666, y: 666 };
    let result = unsafe { GetCursorPos(&mut point) };
    match result.as_bool() {
        true => Ok(point),
        false => Err(eyre!("Get cursor position failed")),
    }
}

//----------------------------------
//      ABSOLUTE MOVE
//----------------------------------

#[allow(dead_code)]
pub fn mouse_move_primary(x: f64, y: f64) -> Result<()> {
    let width = unsafe { GetSystemMetrics(SM_CXSCREEN) } as f64;
    let height = unsafe { GetSystemMetrics(SM_CYSCREEN) } as f64;
    let x = px_to_normalized(x, width);
    let y = px_to_normalized(y, height);
    //println!("{}x{}", x, y);
    sim_mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, 0, x, y)
}

#[allow(dead_code)]
pub fn mouse_move_ex(x: f64, y: f64) -> Result<()> {
    let width = unsafe { GetSystemMetrics(SM_CXVIRTUALSCREEN) } as f64;
    let height = unsafe { GetSystemMetrics(SM_CYVIRTUALSCREEN) } as f64;

    let x = px_to_normalized(x, width);
    let y = px_to_normalized(y, height);
    sim_mouse_event(
        MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
        0,
        x,
        y,
    )
    //.tap_err(|_e| debug!("Mouse simulation failed! Event: Mouse move x:{} y:{}", x, y));
}
#[allow(dead_code)]
pub fn mouse_move_jump(x: i32, y: i32) -> Result<()> {
    let width = unsafe { GetSystemMetrics(SM_CXVIRTUALSCREEN) };
    let height = unsafe { GetSystemMetrics(SM_CYVIRTUALSCREEN) };

    let x = px_to_normalized_i(x, width);
    let y: i32 = px_to_normalized_i(y, height);

    sim_mouse_event(
        MOUSEEVENTF_MOVE
            | MOUSEEVENTF_ABSOLUTE
            | MOUSEEVENTF_VIRTUALDESK
            | MOUSEEVENTF_MOVE_NOCOALESCE,
        0,
        x,
        y,
    )
    //use tap::TapFallible;
    //.tap_err(|_e| debug!("Mouse simulation failed! Event: Mouse jump x:{} y:{}", x, y));
}

#[allow(dead_code)]
pub fn set_cur_pos(x: i32, y: i32) -> bool {
    let res = unsafe { SetCursorPos(x, y) };
    res.as_bool()
}
//----------------------------------
//RELATIVE MOVE
//----------------------------------
#[allow(dead_code)]
pub fn raw_relative_move(mickey_x: i32, mickey_y: i32) -> Result<()> {
    sim_mouse_event(MOUSEEVENTF_MOVE, 0, mickey_x, mickey_y)
}
#[allow(dead_code)]
pub fn raw_relative_move_px(dx: &i32, dy: &i32) -> Result<()> {
    let mickeyx = reverse_acceleration_curve(*dx);
    let mickeyy = reverse_acceleration_curve(*dy);
    raw_relative_move(mickeyx, mickeyy)
}
const ACC_INV: [i32; 16] = [0, 1, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9];
fn reverse_acceleration_curve(px: i32) -> i32 {
    let dist = match px.abs() {
        i if i < 16 => ACC_INV[i as usize],
        n => 6 + (n / 4),
    };
    dist * px.signum()
}

#[allow(dead_code)]
fn px_to_normalized(px: f64, px_max: f64) -> i32 {
    //let px_range = 65535.0 / px_max;
    let mut preccise = (px) * 65535.0;
    preccise /= px_max;
    preccise.round() as i32
}
fn px_to_normalized_i(px: i32, px_max: i32) -> i32 {
    let pixel_range = u16::MAX as f64 / px_max as f64;
    let p = pixel_range * (px as f64 + 0.5);
    p.round() as i32
}

//------------SENT INPUT------------
fn sim_mouse_event(flags: MOUSE_EVENT_FLAGS, data: i32, dx: i32, dy: i32) -> Result<()> {
    //let ex = unsafe { GetMessageExtraInfo() };
    let input = INPUT {
        r#type: INPUT_MOUSE,
        Anonymous: INPUT_0 {
            mi: MOUSEINPUT {
                dx,
                dy,
                mouseData: data,
                dwFlags: flags,
                time: 0,
                dwExtraInfo: JERRY_CLIENT_ID,
            },
        },
    };

    let value = unsafe {
        SendInput(
            &[input as INPUT],
            size_of::<INPUT>()
                .try_into()
                .expect("Could not convert the size of INPUT to i32"),
        )
    };

    if value != 1 {
        Err(eyre!("Native function SendInput failed"))
    } else {
        Ok(())
    }
}
