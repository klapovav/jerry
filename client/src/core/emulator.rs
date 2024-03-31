use super::message_handler::ProcessingError;
use super::Button;
use super::Direction;
use crate::emulation;
#[cfg(target_os = "linux")]
use crate::emulation::linux_k;
#[cfg(target_os = "macos")]
use core_graphics::event::{CGEvent, CGEventTapLocation, ScrollEventUnit};
#[cfg(target_os = "macos")]
use core_graphics::event_source::{CGEventSource, CGEventSourceStateID};
#[cfg(target_os = "windows")]
use emulation::windows_k;
#[cfg(target_os = "macos")]
use enigo::{Enigo, MouseButton, MouseControllable};
#[cfg(target_os = "linux")]
use enigo::{Enigo, MouseButton, MouseControllable};
use eyre::Result;
#[cfg(target_os = "linux")]
use std::os::raw::c_int;
#[cfg(target_os = "linux")]
use std::ptr::null;
#[cfg(target_os = "linux")]
use x11::{xlib, xtest};

pub trait Emulator {
    fn mouse_move_rel(&mut self, dx: i32, dy: i32) -> Result<(), ProcessingError>;
    fn mouse_move_to(&mut self, x: i32, y: i32) -> Result<(), ProcessingError>;
    fn get_cursor(&mut self) -> Result<(i32, i32), ProcessingError>;
    fn mouse_up(&mut self, button: Button) -> Result<(), ProcessingError>;
    fn mouse_down(&mut self, button: Button) -> Result<(), ProcessingError>;
    fn mouse_wheel(&mut self, direction: Direction, amount: f32) -> Result<(), ProcessingError>;
    fn key_down(&mut self, key: u32) -> Result<(), ProcessingError>;
    fn key_up(&mut self, key: u32) -> Result<(), ProcessingError>;
}

//======================================================
/// Emulator trait implementation for testing purposes.
///
///
pub struct NoopEmulator {
    cursor_pos: (i32, i32),
}
impl NoopEmulator {
    pub fn new() -> NoopEmulator {
        Self {
            cursor_pos: (400, 400),
        }
    }
}
impl Emulator for NoopEmulator {
    fn mouse_move_to(&mut self, _x: i32, _y: i32) -> Result<(), ProcessingError> {
        self.cursor_pos = (_x, _y);
        Ok(())
    }
    fn mouse_up(&mut self, _button: Button) -> Result<(), ProcessingError> {
        Ok(())
    }
    fn mouse_down(&mut self, _button: Button) -> Result<(), ProcessingError> {
        Ok(())
    }
    fn mouse_wheel(&mut self, _direction: Direction, _: f32) -> Result<(), ProcessingError> {
        Ok(())
    }
    fn key_down(&mut self, _key: u32) -> Result<(), ProcessingError> {
        Ok(())
    }
    fn key_up(&mut self, _key: u32) -> Result<(), ProcessingError> {
        Ok(())
    }
    fn mouse_move_rel(&mut self, _dx: i32, _dy: i32) -> Result<(), ProcessingError> {
        self.cursor_pos.0 += _dx;
        self.cursor_pos.1 += _dy;
        Ok(())
    }

    fn get_cursor(&mut self) -> Result<(i32, i32), ProcessingError> {
        Ok(self.cursor_pos)
    }
}

///NOTE: wheel acceleration is not implemented
#[cfg(target_os = "windows")]
pub struct WindowsImpl {
    keyboard_emu: windows_k::Keyboard,
    mouse_emu: emulation::windows::Mouse,
}

#[cfg(target_os = "windows")]
#[allow(dead_code)]
impl WindowsImpl {
    pub fn new() -> WindowsImpl {
        Self {
            keyboard_emu: emulation::windows_k::Keyboard::new(),
            mouse_emu: emulation::windows::Mouse::new(),
        }
    }
}
#[cfg(target_os = "windows")]
impl Emulator for WindowsImpl {
    fn mouse_move_rel(&mut self, dx: i32, dy: i32) -> Result<(), ProcessingError> {
        emulation::windows::raw_relative_move_px(&(dx), &(dy))
            .map_err(|_| ProcessingError::FailedToProcess)
    }
    fn mouse_move_to(&mut self, x: i32, y: i32) -> Result<(), ProcessingError> {
        emulation::windows::mouse_move_primary(x as f64, y as f64)
            .map_err(|_| ProcessingError::FailedToProcess)
    }

    fn key_down(&mut self, key: u32) -> Result<(), ProcessingError> {
        let win_vk = u8::try_from(key).map_err(|_e| ProcessingError::UnableToProcess)?;
        self.keyboard_emu
            .key_emu_hybrid(win_vk, true)
            .map_err(|_e| ProcessingError::UnableToProcess)
    }
    fn key_up(&mut self, key: u32) -> Result<(), ProcessingError> {
        let win_vk = u8::try_from(key).unwrap();
        self.keyboard_emu
            .key_emu_hybrid(win_vk, false)
            .map_err(|_e| ProcessingError::UnableToProcess)
    }
    fn mouse_up(&mut self, button: Button) -> Result<(), ProcessingError> {
        self.mouse_emu
            .release(button.into())
            .map_err(|_| ProcessingError::UnableToProcess)
    }

    fn mouse_down(&mut self, button: Button) -> Result<(), ProcessingError> {
        self.mouse_emu
            .press(button.into())
            .map_err(|_| ProcessingError::UnableToProcess)
        // xbutton: Option + [  ] ?
    }

    fn mouse_wheel(&mut self, direction: Direction, am: f32) -> Result<(), ProcessingError> {
        match direction {
            Direction::SCROLL_UP => self.mouse_emu.wheel(am, false),
            Direction::SCROLL_DOWN => self.mouse_emu.wheel(am, false),
            Direction::SCROLL_LEFT => self.mouse_emu.wheel(am, true),
            Direction::SCROLL_RIGHT => self.mouse_emu.wheel(am, true),
        }
        .map_err(|_| ProcessingError::UnableToProcess)
    }

    fn get_cursor(&mut self) -> Result<(i32, i32), ProcessingError> {
        emulation::windows::get_cursor_pos()
            .map_err(|_| ProcessingError::FailedToProcess)
            .map(|p| (p.x, p.y))
    }
}

#[cfg(target_os = "macos")]
pub struct MacImpl {
    enigo: Enigo,
    prev_x: i32,
    prev_y: i32,
}
#[cfg(target_os = "macos")]
impl MacImpl {
    pub fn new() -> MacImpl {
        Self {
            enigo: Enigo::new(),
            prev_x: 0,
            prev_y: 0,
        }
    }
    fn convert_btn(&self, button: Button) -> Option<MouseButton> {
        match button {
            Button::LEFT => Some(MouseButton::Left),
            Button::RIGHT => Some(MouseButton::Right),
            Button::MIDDLE => Some(MouseButton::Middle),
            Button::XBUTTON1 => None,
            Button::XBUTTON2 => None,
        }
    }
    fn scroll_y(&self, amount: f32) -> Result<(), ProcessingError> {
        let amount = (amount / 3f32 + amount.signum() * 1f32) as i32;
        let source = CGEventSource::new(CGEventSourceStateID::CombinedSessionState)
            .map_err(|_| ProcessingError::FailedToProcess)?;
        let event =
            CGEvent::new_scroll_event(source.clone(), ScrollEventUnit::PIXEL, 1, amount, 0, 0)
                .expect("Failed to create a scroll event"); // do not panic
        event.post(CGEventTapLocation::HID);
        Ok(())
    }
    fn scroll_x(&self, amount: f32) -> Result<(), ProcessingError> {
        let amount = (amount / 3f32 + amount.signum() * 1f32) as i32;
        let source = CGEventSource::new(CGEventSourceStateID::CombinedSessionState)
            .map_err(|_| ProcessingError::FailedToProcess)?;
        let event =
            CGEvent::new_scroll_event(source.clone(), ScrollEventUnit::PIXEL, 2, 0, amount, 0)
                .expect("Failed to create a scroll event");
        event.post(CGEventTapLocation::HID);
        Ok(())
    }
}
#[cfg(target_os = "macos")]
impl Emulator for MacImpl {
    fn mouse_move_rel(&mut self, dx: i32, dy: i32) -> Result<(), ProcessingError> {
        self.enigo.mouse_move_relative(dx, dy);
        Ok(())
    }

    fn mouse_move_to(&mut self, x: i32, y: i32) -> Result<(), ProcessingError> {
        self.enigo.mouse_move_to(x, y);
        Ok(())
    }

    fn get_cursor(&mut self) -> Result<(i32, i32), ProcessingError> {
        let loc = self.enigo.mouse_location();
        Ok(loc)
    }

    fn mouse_up(&mut self, button: Button) -> Result<(), ProcessingError> {
        if let Some(btn) = self.convert_btn(button) {
            self.enigo.mouse_up(btn);
            Ok(())
        } else {
            Err(ProcessingError::UnableToProcessPlatformSpecific(
                "Xbutton not suported on Mac".to_owned(),
            ))
        }
    }

    fn mouse_down(&mut self, button: Button) -> Result<(), ProcessingError> {
        if let Some(btn) = self.convert_btn(button) {
            self.enigo.mouse_down(btn);
            Ok(())
        } else {
            Err(ProcessingError::UnableToProcessPlatformSpecific(
                "Xbutton not suported on Mac".to_owned(),
            ))
        }
    }

    fn mouse_wheel(&mut self, direction: Direction, amount: f32) -> Result<(), ProcessingError> {
        match direction {
            Direction::SCROLL_UP | Direction::SCROLL_DOWN => {
                self.scroll_y(amount)?;
            }
            Direction::SCROLL_LEFT | Direction::SCROLL_RIGHT => {
                self.scroll_x(amount)?;
            }
        }
        Ok(())
    }

    //     #[link(name = "Cocoa", kind = "framework")]
    // extern "C" {}
    fn key_down(&mut self, key: u32) -> Result<(), ProcessingError> {
        let key = key as u8;
        match emulation::mac_k::code_from_key(key.into()) {
            Some(code) => {
                let source = CGEventSource::new(CGEventSourceStateID::HIDSystemState)
                    .map_err(|_| ProcessingError::FailedToProcess)?;
                let cg_event = CGEvent::new_keyboard_event(source, code, true)
                    .map_err(|_| ProcessingError::FailedToProcess)?;
                cg_event.post(CGEventTapLocation::HID);
                Ok(())
            }
            None => Err(ProcessingError::UnableToProcess),
        }
    }

    fn key_up(&mut self, key: u32) -> Result<(), ProcessingError> {
        let key = key as u8;
        match emulation::mac_k::code_from_key(key.into()) {
            Some(code) => {
                let source = CGEventSource::new(CGEventSourceStateID::HIDSystemState)
                    .map_err(|_| ProcessingError::FailedToProcess)?;
                let cg_event = CGEvent::new_keyboard_event(source, code, false)
                    .map_err(|_| ProcessingError::FailedToProcess)?;
                cg_event.post(CGEventTapLocation::HID);
                Ok(())
            }
            None => Err(ProcessingError::UnableToProcess),
        }

        // let code = emulation::mac_k::code_from_key(*key)?;
        // CGEvent::new_keyboard_event(source, code, false).ok();
        // cg_event.post(CGEventTapLocation::HID);
        // Ok(())
    }
}
#[cfg(target_os = "linux")]
enum EventType {
    WheelY(f32),
    WheelX(f32),
    KeyDown(u32),
    KeyUp(u32),
}
#[cfg(target_os = "linux")]
pub struct LinuxImpl {
    enigo: Enigo,
    wheel_x: i32,
    wheel_y: i32,
}
#[cfg(target_os = "linux")]
impl LinuxImpl {
    pub fn new() -> Self {
        Self {
            enigo: Enigo::new(),
            wheel_x: 0,
            wheel_y: 0,
        }
    }
    fn convert_btn(&self, button: Button) -> MouseButton {
        match button {
            Button::LEFT => MouseButton::Left,
            Button::RIGHT => MouseButton::Right,
            Button::MIDDLE => MouseButton::Middle,
            Button::XBUTTON1 => MouseButton::Forward,
            Button::XBUTTON2 => MouseButton::Back,
        }
    }
    fn simulate(&mut self, event_type: EventType) -> Result<(), ProcessingError> {
        unsafe {
            let dpy = xlib::XOpenDisplay(null());
            if dpy.is_null() {
                return Err(ProcessingError::FailedToProcess); // "Can't open X11 Display"
            }
            match self.send_native(event_type, dpy) {
                Some(_) => {
                    xlib::XFlush(dpy);
                    xlib::XSync(dpy, 0);
                    xlib::XCloseDisplay(dpy);
                    Ok(())
                }
                None => {
                    xlib::XCloseDisplay(dpy);
                    Err(ProcessingError::FailedToProcess)
                }
            }
        }
    }
    unsafe fn send_native(
        &mut self,
        event_type: EventType,
        display: *mut xlib::Display,
    ) -> Option<()> {
        let mut result: c_int = 1;
        match event_type {
            EventType::WheelY(amount) => {
                self.wheel_y += amount as i32;
                while self.wheel_y.abs() > 30 {
                    let step = 30 * self.wheel_y.signum();
                    let code = if self.wheel_y.signum() > 0 { 4 } else { 5 };
                    result &= xtest::XTestFakeButtonEvent(display, code, 1 as c_int, 0)
                        & xtest::XTestFakeButtonEvent(display, code, 0 as c_int, 0);
                    self.wheel_y -= step;
                }
            }
            EventType::WheelX(amount) => {
                self.wheel_x += amount as i32;
                while self.wheel_x.abs() > 30 {
                    let step = 30 * self.wheel_x.signum();
                    let code = if self.wheel_x.signum() > 0 { 7 } else { 6 };
                    result &= xtest::XTestFakeButtonEvent(display, code, 1 as c_int, 0)
                        & xtest::XTestFakeButtonEvent(display, code, 0 as c_int, 0);
                    self.wheel_x -= step;
                }
            }
            EventType::KeyDown(key) => {
                let key = key as u8;
                let code = linux_k::code_from_key(key.into())?;
                result &= xtest::XTestFakeKeyEvent(display, code, 1 as c_int, 0);
            }
            EventType::KeyUp(key) => {
                let key = key as u8;
                let code = linux_k::code_from_key(key.into())?;
                result &= xtest::XTestFakeKeyEvent(display, code, 0 as c_int, 0);
            }
        }
        if result == 1 {
            Some(())
        } else {
            None
        }
    }
}
#[cfg(target_os = "linux")]
impl Emulator for LinuxImpl {
    fn mouse_move_rel(&mut self, dx: i32, dy: i32) -> Result<(), ProcessingError> {
        self.enigo.mouse_move_relative(dx, dy);
        Ok(())
    }

    fn mouse_move_to(&mut self, x: i32, y: i32) -> Result<(), ProcessingError> {
        self.enigo.mouse_move_to(x, y);
        Ok(())
    }

    fn get_cursor(&mut self) -> Result<(i32, i32), ProcessingError> {
        let loc = self.enigo.mouse_location();
        Ok(loc)
    }

    fn mouse_up(&mut self, button: Button) -> Result<(), ProcessingError> {
        let btn = self.convert_btn(button);
        self.enigo.mouse_up(btn);
        Ok(())
    }

    fn mouse_down(&mut self, button: Button) -> Result<(), ProcessingError> {
        let btn = self.convert_btn(button);
        self.enigo.mouse_down(btn);
        Ok(())
    }

    fn mouse_wheel(&mut self, direction: Direction, amount: f32) -> Result<(), ProcessingError> {
        let event = match direction {
            Direction::SCROLL_UP | Direction::SCROLL_DOWN => EventType::WheelY(amount),
            Direction::SCROLL_LEFT | Direction::SCROLL_RIGHT => EventType::WheelX(amount),
        };
        self.simulate(event)
    }

    fn key_down(&mut self, key: u32) -> Result<(), ProcessingError> {
        self.simulate(EventType::KeyDown(key))
    }

    fn key_up(&mut self, key: u32) -> Result<(), ProcessingError> {
        self.simulate(EventType::KeyUp(key))
    }
}
