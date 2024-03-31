use eyre::{eyre, Result};
use std::collections::HashMap;
use std::mem::size_of;
use tracing::debug;
use windows::Win32::UI::Input::KeyboardAndMouse::{
    GetKeyboardLayout, MapVirtualKeyExA, SendInput, INPUT, INPUT_0, INPUT_KEYBOARD, KEYBDINPUT,
    KEYBD_EVENT_FLAGS, KEYEVENTF_EXTENDEDKEY, KEYEVENTF_KEYUP, KEYEVENTF_SCANCODE,
    MAP_VIRTUAL_KEY_TYPE, VIRTUAL_KEY,
};

pub const VK_LAYOUT_DEPENDENT: VIRTUAL_KEY = VIRTUAL_KEY(0u16);
pub const _X__: u8 = 0x00;
pub const EXT0: u8 = 0xE0;
pub const EXT1: u8 = 0xE1;

use windows::Win32::UI::Input::KeyboardAndMouse::{
    VK__none_,
    //VK_0, VK_1, VK_2, VK_3, VK_4, VK_5,VK_6,VK_7,VK_8,VK_9,VK_A,VK_ABNT_C1,VK_ABNT_C2,VK_ACCEPT,
    VK_ADD,
    VK_APPS,
    //VK_BACK,
    //VK_CAPITAL,
    VK_CLEAR,
    VK_DECIMAL,
    VK_DELETE,
    VK_DIVIDE,
    VK_DOWN,
    // VK_E,VK_B, VK_ATTN, VK_BROWSER_BACK,VK_BROWSER_FAVORITES,VK_BROWSER_FORWARD,
    // VK_BROWSER_HOME,VK_BROWSER_REFRESH,VK_BROWSER_SEARCH,VK_BROWSER_STOP,VK_C,VK_CANCEL, VK_CONTROL,
    // VK_CONVERT, VK_CRSEL, VK_D, VK_DBE_ALPHANUMERIC, VK_DBE_CODEINPUT, VK_DBE_DBCSCHAR,
    // VK_DBE_DETERMINESTRING, VK_DBE_ENTERDLGCONVERSIONMODE, VK_DBE_ENTERIMECONFIGMODE,
    // VK_DBE_ENTERWORDREGISTERMODE, VK_DBE_FLUSHSTRING, VK_DBE_HIRAGANA, VK_DBE_KATAKANA,
    // VK_DBE_NOCODEINPUT, VK_DBE_NOROMAN, VK_DBE_ROMAN, VK_DBE_SBCSCHAR,
    VK_END,
    //VK_ESCAPE,
    VK_F1,
    VK_F10,
    VK_F11,
    VK_F12,
    VK_F2,
    VK_F3,
    VK_F4,
    VK_F5,
    VK_F6,
    VK_F7,
    VK_F8,
    VK_F9,
    //VK_F13,VK_F14,VK_F15,VK_F16,VK_F17,VK_F18,VK_F19,VK_F20,VK_F21,VK_F22,VK_F23,VK_F24,
    VK_HOME,
    VK_INSERT,
    VK_LCONTROL,
    VK_LEFT,
    VK_LMENU,
    //VK_LSHIFT,
    VK_LWIN,
    VK_MEDIA_NEXT_TRACK,
    VK_MEDIA_PLAY_PAUSE,
    VK_MEDIA_PREV_TRACK,
    VK_MEDIA_STOP,
    VK_MULTIPLY,
    // VK_MENU, VK_MODECHANGE, VK_N, VK_NAVIGATION_ACCEPT, VK_NAVIGATION_CANCEL, VK_NAVIGATION_DOWN,
    // VK_NAVIGATION_LEFT, VK_NAVIGATION_MENU, VK_NAVIGATION_RIGHT, VK_NAVIGATION_UP, VK_NAVIGATION_VIEW, VK_NONAME, VK_NONCONVERT,
    VK_NEXT,
    VK_NUMLOCK,
    VK_NUMPAD0,
    VK_NUMPAD1,
    VK_NUMPAD2,
    VK_NUMPAD3,
    VK_NUMPAD4,
    VK_NUMPAD5,
    VK_NUMPAD6,
    VK_NUMPAD7,
    VK_NUMPAD8,
    VK_NUMPAD9,
    // VK_O, VK_OEM_1, VK_OEM_102, VK_OEM_2, VK_OEM_3, VK_OEM_4, VK_OEM_5, VK_OEM_6, VK_OEM_7, VK_OEM_8,
    // VK_OEM_ATTN, VK_OEM_AUTO, VK_OEM_AX, VK_OEM_BACKTAB, VK_OEM_CLEAR, VK_OEM_COMMA, VK_OEM_COPY,
    // VK_OEM_CUSEL, VK_OEM_ENLW, VK_OEM_FINISH, VK_OEM_FJ_JISHO, VK_OEM_FJ_LOYA, VK_OEM_FJ_MASSHOU,
    // VK_OEM_FJ_ROYA, VK_OEM_FJ_TOUROKU, VK_OEM_JUMP, VK_OEM_MINUS, VK_OEM_NEC_EQUAL, VK_OEM_PA1,
    // VK_OEM_PA2, VK_OEM_PA3, VK_OEM_PERIOD, VK_OEM_PLUS, VK_OEM_RESET, VK_OEM_WSCTRL, VK_P, VK_PA1,
    // VK_PACKET, VK_PLAY, VK_PROCESSKEY, VK_Q, VK_R, VK_RBUTTON,
    VK_PAUSE,
    VK_PRINT,
    VK_PRIOR,
    VK_RCONTROL,
    VK_RETURN,
    VK_RIGHT,
    VK_RMENU,
    //VK_RSHIFT,
    VK_RWIN,
    VK_SCROLL,
    //VK_S, VK_SELECT, VK_SEPARATOR, VK_SHIFT, VK_SLEEP, VK_SNAPSHOT,
    VK_SPACE,
    VK_SUBTRACT,
    //VK_TAB,
    VK_UP,
    VK_VOLUME_DOWN,
    VK_VOLUME_MUTE,
    VK_VOLUME_UP,
};

use windows::Win32::UI::WindowsAndMessaging::{
    // GetCursorPos,  GetSystemMetrics, SetCursorPos,
    // SM_CXSCREEN, SM_CYSCREEN,
    GetForegroundWindow,
    //GetMessageExtraInfo,
    //SetMessageExtraInfo,
    GetWindowThreadProcessId,
};

use crate::JERRY_CLIENT_ID;

#[derive(Debug)]
pub struct WinKey(u16, u8, VIRTUAL_KEY);
#[derive(Debug)]
struct ScanCode(u16, VIRTUAL_KEY, KEYBD_EVENT_FLAGS);

fn keybd_event(scan: u16, vk: VIRTUAL_KEY, flags: KEYBD_EVENT_FLAGS) -> Result<()> {
    //let extra = unsafe { GetMessageExtraInfo() };
    let input = INPUT {
        r#type: INPUT_KEYBOARD,
        Anonymous: INPUT_0 {
            ki: KEYBDINPUT {
                wVk: vk,
                wScan: scan,
                dwFlags: flags,
                time: 550,
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
        Err(eyre!("Send Input failed"))
    } else {
        Ok(())
    }
}
fn key_to_layout_key(raw_data: &WinKey) -> ScanCode {
    let scan = raw_data.0;
    let flags = match raw_data.1 {
        0 => KEYBD_EVENT_FLAGS::default() | KEYEVENTF_EXTENDEDKEY,
        _ => KEYBD_EVENT_FLAGS::default(),
    };
    //layout dependent virtual key
    let vk = match raw_data.2 {
        VK_LAYOUT_DEPENDENT => scan_to_vk(raw_data.0, raw_data.1),
        _ => raw_data.2,
    };

    ScanCode(scan, vk, flags)
}
fn scan_to_vk(scan: u16, ext: u8) -> VIRTUAL_KEY {
    let hkl = unsafe {
        let id_thread = GetWindowThreadProcessId(GetForegroundWindow(), None);
        let hkl: windows::Win32::UI::TextServices::HKL = GetKeyboardLayout(id_thread);
        debug!("thread id {:?}, hkl {:?}", id_thread, hkl);
        //hkl: The return value is the input locale identifier for the thread.
        //The low word contains a Language Identifier for the input language
        // the high word contains a device handle to the physical layout of the keyboard.

        hkl
    };
    let scancode = (ext as u16) << 8 | scan;
    let layout_vk = unsafe {
        //ucode param: Starting with Windows Vista, the high byte of the uCode value can contain either 0xe0 or 0xe1
        //to specify the extended scan code.
        MapVirtualKeyExA(scancode.into(), MAP_VIRTUAL_KEY_TYPE(3), hkl)
    };
    VIRTUAL_KEY(u16::try_from(layout_vk).unwrap_or(0))
}

macro_rules! collection {
    // map-like
    ($($k:expr => $v:expr),* $(,)?) => {{
        use std::iter::{Iterator, IntoIterator};
        Iterator::collect(IntoIterator::into_iter([$(($k, $v),)*]))
    }};
    // set-like
    ($($v:expr),* $(,)?) => {{
        use std::iter::{Iterator, IntoIterator};
        Iterator::collect(IntoIterator::into_iter([$($v,)*]))
    }};
}

//-------------------------
pub struct Keyboard {
    keys: HashMap<u8, WinKey>,
}
#[allow(dead_code)]
impl Keyboard {
    pub fn new() -> Keyboard {
        //let _prev_info = unsafe { SetMessageExtraInfo(LPARAM(JERRY_CLIENT_ID)) };
        Self {
            keys: Self::create_hash(),
        }
    }
    pub fn key_down(&self, win_vk: u8) -> Result<()> {
        self.key(win_vk, true)
    }

    pub fn key_up(&self, win_vk: u8) -> Result<()> {
        self.key(win_vk, false)
    }
    pub fn key_emu_hybrid(&self, win_vk: u8, pressed: bool) -> Result<()> {
        let a = self
            .keys
            .get(&win_vk)
            .ok_or(eyre!("Key with id {} is not supported", win_vk))?;

        let scan = a.0;
        let ext = a.1;
        let vk = a.2;

        let kb_def = KEYBD_EVENT_FLAGS(0);
        let key_flags = match (ext, vk) {
            (0, VK_LAYOUT_DEPENDENT) => kb_def | KEYEVENTF_SCANCODE,
            (EXT0, VK_LAYOUT_DEPENDENT) => kb_def | KEYEVENTF_SCANCODE | KEYEVENTF_EXTENDEDKEY,
            (EXT0, _) => kb_def | KEYEVENTF_EXTENDEDKEY,
            (0, _) => kb_def,
            (_, _) => return Err(eyre!("Extended scan code not valid")),
        };

        match pressed {
            true => keybd_event(scan, vk, key_flags),
            false => keybd_event(scan, vk, key_flags | KEYEVENTF_KEYUP),
        }
    }
    fn key(&self, win_vk: u8, pressed: bool) -> Result<()> {
        if VIRTUAL_KEY(win_vk.into()) == VK__none_ {
            return Ok(());
        };
        let a = self.keys.get(&win_vk).map(key_to_layout_key);

        match a {
            Some(sc) => match pressed {
                true => keybd_event(sc.0, sc.1, sc.2),
                false => keybd_event(sc.0, sc.1, sc.2 | KEYEVENTF_KEYUP),
            },

            None => Err(eyre!("Could not simulate key action")), //not included in hashmap
        }
    }

    fn create_hash() -> HashMap<u8, WinKey> {
        let s: HashMap<_, _> = collection! {
            // VK(US_LAYOUT) => (SC,SC_EX,VK)
            0x30 =>WinKey(0x0B, _X__, VK_LAYOUT_DEPENDENT),     // VK_0
            0x31 =>WinKey(0x02, _X__, VK_LAYOUT_DEPENDENT),     // VK_1
            0x32 =>WinKey(0x03, _X__, VK_LAYOUT_DEPENDENT),     // VK_2
            0x33 =>WinKey(0x04, _X__, VK_LAYOUT_DEPENDENT),     // VK_3
            0x34 =>WinKey(0x05, _X__, VK_LAYOUT_DEPENDENT),     // VK_4
            0x35 =>WinKey(0x06, _X__, VK_LAYOUT_DEPENDENT),     // VK_5
            0x36 =>WinKey(0x07, _X__, VK_LAYOUT_DEPENDENT),     // VK_6
            0x37 =>WinKey(0x08, _X__, VK_LAYOUT_DEPENDENT),     // VK_7
            0x38 =>WinKey(0x09, _X__, VK_LAYOUT_DEPENDENT),     // VK_8
            0x39 =>WinKey(0x0A, _X__, VK_LAYOUT_DEPENDENT),     // VK_9

            0x41 => WinKey(0x1E, _X__, VK_LAYOUT_DEPENDENT),    // VK_A = 65
            0x42 => WinKey(0x30, _X__, VK_LAYOUT_DEPENDENT),    // VK_B
            0x43 => WinKey(0x2E, _X__, VK_LAYOUT_DEPENDENT),    // VK_C
            0x44 => WinKey(0x20, _X__, VK_LAYOUT_DEPENDENT),    // VK_D
            0x45 => WinKey(0x12, _X__, VK_LAYOUT_DEPENDENT),    // VK_E
            0x46 => WinKey(0x21, _X__, VK_LAYOUT_DEPENDENT),    // VK_F
            0x47 => WinKey(0x22, _X__, VK_LAYOUT_DEPENDENT),    // VK_G
            0x48 => WinKey(0x23, _X__, VK_LAYOUT_DEPENDENT),    // VK_H
            0x49 => WinKey(0x17, _X__, VK_LAYOUT_DEPENDENT),    // VK_I
            0x4A => WinKey(0x24, _X__, VK_LAYOUT_DEPENDENT),    // VK_J
            0x4B => WinKey(0x25, _X__, VK_LAYOUT_DEPENDENT),    // VK_K
            0x4C => WinKey(0x26, _X__, VK_LAYOUT_DEPENDENT),    // VK_L
            0x4D => WinKey(0x32, _X__, VK_LAYOUT_DEPENDENT),    // VK_M
            0x4E => WinKey(0x31, _X__, VK_LAYOUT_DEPENDENT),    // VK_N
            0x4F => WinKey(0x18, _X__, VK_LAYOUT_DEPENDENT),    // VK_O
            0x50 => WinKey(0x19, _X__, VK_LAYOUT_DEPENDENT),    // VK_P
            0x51 => WinKey(0x10, _X__, VK_LAYOUT_DEPENDENT),    // VK_Q
            0x52 => WinKey(0x13, _X__, VK_LAYOUT_DEPENDENT),    // VK_R
            0x53 => WinKey(0x1F, _X__, VK_LAYOUT_DEPENDENT),    // VK_S
            0x54 => WinKey(0x14, _X__, VK_LAYOUT_DEPENDENT),    // VK_T
            0x55 => WinKey(0x16, _X__, VK_LAYOUT_DEPENDENT),    // VK_U
            0x56 => WinKey(0x2F, _X__, VK_LAYOUT_DEPENDENT),    // VK_V
            0x57 => WinKey(0x11, _X__, VK_LAYOUT_DEPENDENT),    // VK_W
            0x58 => WinKey(0x2D, _X__, VK_LAYOUT_DEPENDENT),    // VK_X
            0x59 => WinKey(0x15, _X__, VK_LAYOUT_DEPENDENT),    // VK_Y
            0x5A => WinKey(0x2C, _X__, VK_LAYOUT_DEPENDENT),    // VK_Z = 90

            0x70 => WinKey(0x3B, _X__, VK_F1),                  // VK_F1
            0x71 => WinKey(0x3C, _X__, VK_F2),                  // VK_F2
            0x72 => WinKey(0x3D, _X__, VK_F3),                  // VK_F3
            0x73 => WinKey(0x3E, _X__, VK_F4),                  // VK_F4
            0x74 => WinKey(0x3F, _X__, VK_F5),                  // VK_F5
            0x75 => WinKey(0x40, _X__, VK_F6),                  // VK_F6
            0x76 => WinKey(0x41, _X__, VK_F7),                  // VK_F7
            0x77 => WinKey(0x42, _X__, VK_F8),                  // VK_F8
            0x78 => WinKey(0x43, _X__, VK_F9),                  // VK_F9
            0x79 => WinKey(0x44, _X__, VK_F10),                 // VK_F10
            0x7A => WinKey(0x57, _X__, VK_F11),                 // VK_F11
            0x7B => WinKey(0x58, _X__, VK_F12),                 // VK_F12

            //NUMPAD
            0x60 => WinKey(0x52, _X__,  VK_NUMPAD0),
            0x61 => WinKey(0x4F, _X__,  VK_NUMPAD1),
            0x62 => WinKey(0x50, _X__,  VK_NUMPAD2),
            0x63 => WinKey(0x51, _X__,  VK_NUMPAD3),
            0x64 => WinKey(0x4B, _X__,  VK_NUMPAD4),
            0x65 => WinKey(0x4C, _X__,  VK_NUMPAD5),
            0x66 => WinKey(0x4D, _X__,  VK_NUMPAD6),
            0x67 => WinKey(0x47, _X__,  VK_NUMPAD7),
            0x68 => WinKey(0x48, _X__,  VK_NUMPAD8),
            0x69 => WinKey(0x49, _X__,  VK_NUMPAD9),
            0x6A => WinKey(0x37, _X__, VK_MULTIPLY),
            0x6B => WinKey(0x4E, _X__, VK_ADD),
           //0x6C VK_SEPARATOR
            0x6D => WinKey(0x4A, _X__, VK_SUBTRACT),
            0x6E => WinKey(0x53, _X__, VK_DECIMAL),
            0x6F => WinKey(0x35, EXT0, VK_DIVIDE),
            0x0C => WinKey(0x4C, _X__, VK_CLEAR),
            0x90 => WinKey(0x45, EXT0, VK_NUMLOCK),

            //NAV KEYS
            0x26 =>WinKey(0x48, EXT0, VK_UP),
            0x25 =>WinKey(0x4B, EXT0, VK_LEFT),
            0x27 =>WinKey(0x4D, EXT0, VK_RIGHT),
            0x28 =>WinKey(0x50, EXT0, VK_DOWN),

            0x2D => WinKey(0x52, EXT0, VK_INSERT),
            0x23 => WinKey(0x4F, EXT0, VK_END),
            0x2E => WinKey(0x53, EXT0, VK_DELETE),
            0x24 => WinKey(0x47, EXT0, VK_HOME),
            0x21 => WinKey(0x49, EXT0, VK_PRIOR),
            0x22 => WinKey(0x51, EXT0, VK_NEXT),


            0xBA=> WinKey(0x27, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_1, VK_Semicolon
            0xBB=> WinKey(0x0D, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_PLUS
            0xBC=> WinKey(0x33, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_COMMA
            0xBD=> WinKey(0x0C, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_MINUS
            0xBE=> WinKey(0x34, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_PERIOD
            0xBF=> WinKey(0x35, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_2, VK_Slash
            0xC0=> WinKey(0x29, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_3, VK_Grave
            0xDB=> WinKey(0x1A, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_4, VK_LeftBracket
            0xDC=> WinKey(0x2B, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_5, VK_Backslash
            0xDD=> WinKey(0x1B, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_6, VK_RightBracket
            0xDE=> WinKey(0x28, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM_7, VK_Quote
            0xE2=> WinKey(0x56, _X__, VK_LAYOUT_DEPENDENT),            //VK_OEM102, VK_IntlBackslash

            0x2C=> WinKey(0x37, EXT0, VK_PRINT),
            0x91=> WinKey(0x46, _X__, VK_SCROLL),
            0x13=> WinKey(0x1D, EXT1, VK_PAUSE),

            0x1B => WinKey(0x01, _X__, VK_LAYOUT_DEPENDENT),
            0x09 => WinKey(0x0F, _X__, VK_LAYOUT_DEPENDENT),
            0x14 => WinKey(0x3A, _X__, VK_LAYOUT_DEPENDENT),     //The CAPS LOCK key, nebo
            0xA0 => WinKey(0x2A, _X__, VK_LAYOUT_DEPENDENT),
            0xA2 => WinKey(0x1D, _X__, VK_LCONTROL),
            0x5B => WinKey(0x5B, EXT0, VK_LWIN),
            0xA4 => WinKey(0x38, _X__, VK_LMENU),               //The left ALT key
            0x20 => WinKey(0x39, _X__, VK_SPACE),
            0xA5 => WinKey(0x38, EXT0, VK_RMENU),               //The right ALT key  (Option ... macOS)
            0x5D => WinKey(0x5D, EXT0, VK_APPS),                //The Application key (context menu)
            0x5C => WinKey(0x5C, EXT0, VK_RWIN),
            0xA3 => WinKey(0x1D, EXT0, VK_RCONTROL),
            0xA1 => WinKey(0x36, _X__, VK_LAYOUT_DEPENDENT),
            0x0D => WinKey(0x1C, _X__, VK_LAYOUT_DEPENDENT),      //The RETURN key
            0x08 => WinKey(0x0E, _X__, VK_LAYOUT_DEPENDENT),      //The BACKSPACE key

            //VIRTUAL KEYS
            0xAD => WinKey(0x00, _X__, VK_VOLUME_MUTE),
            0xAE => WinKey(0x00, _X__, VK_VOLUME_DOWN),
            0xAF => WinKey(0x00, _X__, VK_VOLUME_UP),
            0xB0 => WinKey(0x00, _X__, VK_MEDIA_NEXT_TRACK),
            0xB1 => WinKey(0x00, _X__, VK_MEDIA_PREV_TRACK),
            0xB2 => WinKey(0x00, _X__, VK_MEDIA_STOP),
            0xB3 => WinKey(0x00, _X__, VK_MEDIA_PLAY_PAUSE),

            0x0A => WinKey(0x1C, EXT0, VK_RETURN),

        };
        s
    }
}
