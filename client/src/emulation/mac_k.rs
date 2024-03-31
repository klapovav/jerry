use super::JKey;
use core_graphics::event::CGKeyCode;

const ALT: CGKeyCode = 58;
const ALT_GR: CGKeyCode = 61;
const BACKSPACE: CGKeyCode = 51;
const CAPS_LOCK: CGKeyCode = 57;
const CONTROL_LEFT: CGKeyCode = 59;
const CONTROL_RIGHT: CGKeyCode = 62;
const DOWN_ARROW: CGKeyCode = 125;
const ESCAPE: CGKeyCode = 53;
const F1: CGKeyCode = 122;
const F10: CGKeyCode = 109;
const F11: CGKeyCode = 103;
const F12: CGKeyCode = 111;
const F2: CGKeyCode = 120;
const F3: CGKeyCode = 99;
const F4: CGKeyCode = 118;
const F5: CGKeyCode = 96;
const F6: CGKeyCode = 97;
const F7: CGKeyCode = 98;
const F8: CGKeyCode = 100;
const F9: CGKeyCode = 101;
// const F13:CGKeyCode = 105;//printscreen pos
// const F14:CGKeyCode = 107;//scroll
// const F15:CGKeyCode = 113;//pause/break

const FUNCTION: CGKeyCode = 63;
const LEFT_ARROW: CGKeyCode = 123;
const META_LEFT: CGKeyCode = 55;
const META_RIGHT: CGKeyCode = 54;
const RETURN: CGKeyCode = 36;
const RIGHT_ARROW: CGKeyCode = 124;
const SHIFT_LEFT: CGKeyCode = 56;
const SHIFT_RIGHT: CGKeyCode = 60;
const SPACE: CGKeyCode = 49;
const TAB: CGKeyCode = 48;
const UP_ARROW: CGKeyCode = 126;
const BACK_QUOTE: CGKeyCode = 50; //0x32, (ISO oem102)
const GRAVE: CGKeyCode = 10;
const NUM1: CGKeyCode = 18;
const NUM2: CGKeyCode = 19;
const NUM3: CGKeyCode = 20;
const NUM4: CGKeyCode = 21;
const NUM5: CGKeyCode = 23;
const NUM6: CGKeyCode = 22;
const NUM7: CGKeyCode = 26;
const NUM8: CGKeyCode = 28;
const NUM9: CGKeyCode = 25;
const NUM0: CGKeyCode = 29;

const KP_DECIMAL: CGKeyCode = 0x41;
const KP_MULTIPLY: CGKeyCode = 0x43;
const KP_PLUS: CGKeyCode = 0x45;
const KP_CLEAR: CGKeyCode = 0x47;
const KP_DIVIDE: CGKeyCode = 0x4B;
const KP_ENTER: CGKeyCode = 0x4C;
const KP_MINUS: CGKeyCode = 0x4E;
//const KP_EQUALS: CGKeyCode = 0x51;
const KP_0: CGKeyCode = 0x52;
const KP_1: CGKeyCode = 0x53;
const KP_2: CGKeyCode = 0x54;
const KP_3: CGKeyCode = 0x55;
const KP_4: CGKeyCode = 0x56;
const KP_5: CGKeyCode = 0x57;
const KP_6: CGKeyCode = 0x58;
const KP_7: CGKeyCode = 0x59;
const KP_8: CGKeyCode = 0x5B;
const KP_9: CGKeyCode = 0x5C;

const MINUS: CGKeyCode = 27;
const EQUAL: CGKeyCode = 24;
const KEY_Q: CGKeyCode = 12;
const KEY_W: CGKeyCode = 13;
const KEY_E: CGKeyCode = 14;
const KEY_R: CGKeyCode = 15;
const KEY_T: CGKeyCode = 17;
const KEY_Y: CGKeyCode = 16;
const KEY_U: CGKeyCode = 32;
const KEY_I: CGKeyCode = 34;
const KEY_O: CGKeyCode = 31;
const KEY_P: CGKeyCode = 35;
const LEFT_BRACKET: CGKeyCode = 33;
const RIGHT_BRACKET: CGKeyCode = 30;
const KEY_A: CGKeyCode = 0;
const KEY_S: CGKeyCode = 1;
const KEY_D: CGKeyCode = 2;
const KEY_F: CGKeyCode = 3;
const KEY_G: CGKeyCode = 5;
const KEY_H: CGKeyCode = 4;
const KEY_J: CGKeyCode = 38;
const KEY_K: CGKeyCode = 40;
const KEY_L: CGKeyCode = 37;
const SEMI_COLON: CGKeyCode = 41;
const QUOTE: CGKeyCode = 39;
const BACK_SLASH: CGKeyCode = 42; //0x2A backslash u enteru
const KEY_Z: CGKeyCode = 6;
const KEY_X: CGKeyCode = 7;
const KEY_C: CGKeyCode = 8;
const KEY_V: CGKeyCode = 9;
const KEY_B: CGKeyCode = 11;
const KEY_N: CGKeyCode = 45;
const KEY_M: CGKeyCode = 46;
const COMMA: CGKeyCode = 43;
const DOT: CGKeyCode = 47;
const SLASH: CGKeyCode = 44;
const INSERT: CGKeyCode = 114;
const DELETE: CGKeyCode = 117;

const END: CGKeyCode = 119;
const HOME: CGKeyCode = 115;
const PAGEUP: CGKeyCode = 116;
const PAGEDOWN: CGKeyCode = 121;

pub fn code_from_key(key: JKey) -> Option<CGKeyCode> {
    match key {
        JKey::Alt => Some(ALT),
        JKey::AltGr => Some(ALT_GR),
        JKey::Backspace => Some(BACKSPACE),
        JKey::CapsLock => Some(CAPS_LOCK),
        JKey::ControlLeft => Some(CONTROL_LEFT),
        JKey::ControlRight => Some(CONTROL_RIGHT),
        JKey::DownArrow => Some(DOWN_ARROW),
        JKey::Escape => Some(ESCAPE),
        JKey::F1 => Some(F1),
        JKey::F10 => Some(F10),
        JKey::F11 => Some(F11),
        JKey::F12 => Some(F12),
        JKey::F2 => Some(F2),
        JKey::F3 => Some(F3),
        JKey::F4 => Some(F4),
        JKey::F5 => Some(F5),
        JKey::F6 => Some(F6),
        JKey::F7 => Some(F7),
        JKey::F8 => Some(F8),
        JKey::F9 => Some(F9),
        JKey::LeftArrow => Some(LEFT_ARROW),
        JKey::MetaLeft => Some(META_LEFT),
        JKey::MetaRight => Some(META_RIGHT),
        JKey::Return => Some(RETURN),
        JKey::RightArrow => Some(RIGHT_ARROW),
        JKey::ShiftLeft => Some(SHIFT_LEFT),
        JKey::ShiftRight => Some(SHIFT_RIGHT),
        JKey::Space => Some(SPACE),
        JKey::Tab => Some(TAB),
        JKey::UpArrow => Some(UP_ARROW),
        JKey::BackQuote => Some(GRAVE),
        JKey::Num1 => Some(NUM1),
        JKey::Num2 => Some(NUM2),
        JKey::Num3 => Some(NUM3),
        JKey::Num4 => Some(NUM4),
        JKey::Num5 => Some(NUM5),
        JKey::Num6 => Some(NUM6),
        JKey::Num7 => Some(NUM7),
        JKey::Num8 => Some(NUM8),
        JKey::Num9 => Some(NUM9),
        JKey::Num0 => Some(NUM0),

        JKey::Kp0 => Some(KP_0),
        JKey::Kp1 => Some(KP_1),
        JKey::Kp2 => Some(KP_2),
        JKey::Kp3 => Some(KP_3),
        JKey::Kp4 => Some(KP_4),
        JKey::Kp5 => Some(KP_5),
        JKey::Kp6 => Some(KP_6),
        JKey::Kp7 => Some(KP_7),
        JKey::Kp8 => Some(KP_8),
        JKey::Kp9 => Some(KP_9),
        JKey::KpDelete => Some(KP_DECIMAL),
        JKey::KpReturn => Some(KP_ENTER),
        JKey::KpPlus => Some(KP_PLUS),
        JKey::KpMinus => Some(KP_MINUS),
        JKey::KpMultiply => Some(KP_MULTIPLY),
        JKey::KpDivide => Some(KP_DIVIDE),
        JKey::NumLock => Some(KP_CLEAR),

        //JKey::Clear => Some(KP_Clear),
        // const KP_Equals: CGKeyCode = 0x51;
        // const KP_Clear: CGKeyCode = 0x47;
        JKey::Minus => Some(MINUS),
        JKey::Equal => Some(EQUAL),
        JKey::Q => Some(KEY_Q),
        JKey::W => Some(KEY_W),
        JKey::E => Some(KEY_E),
        JKey::R => Some(KEY_R),
        JKey::T => Some(KEY_T),
        JKey::Y => Some(KEY_Y),
        JKey::U => Some(KEY_U),
        JKey::I => Some(KEY_I),
        JKey::O => Some(KEY_O),
        JKey::P => Some(KEY_P),
        JKey::LeftBracket => Some(LEFT_BRACKET),
        JKey::RightBracket => Some(RIGHT_BRACKET),
        JKey::A => Some(KEY_A),
        JKey::S => Some(KEY_S),
        JKey::D => Some(KEY_D),
        JKey::F => Some(KEY_F),
        JKey::G => Some(KEY_G),
        JKey::H => Some(KEY_H),
        JKey::J => Some(KEY_J),
        JKey::K => Some(KEY_K),
        JKey::L => Some(KEY_L),
        JKey::SemiColon => Some(SEMI_COLON),
        JKey::Quote => Some(QUOTE),
        JKey::BackSlash => Some(BACK_SLASH),
        JKey::Z => Some(KEY_Z),
        JKey::X => Some(KEY_X),
        JKey::C => Some(KEY_C),
        JKey::V => Some(KEY_V),
        JKey::B => Some(KEY_B),
        JKey::N => Some(KEY_N),
        JKey::M => Some(KEY_M),
        JKey::Comma => Some(COMMA),
        JKey::Dot => Some(DOT),
        JKey::Slash => Some(SLASH),
        // JKey::Function => Some(FUNCTION),
        JKey::IntlBackslash => Some(BACK_QUOTE),
        //JKey::Delete => Some(DELETE),
        JKey::End => Some(END),
        JKey::Home => Some(HOME),
        JKey::PageDown => Some(PAGEDOWN),
        JKey::PageUp => Some(PAGEUP),
        JKey::PrintScreen => None, //Some(F13),//pos ok
        JKey::ScrollLock => None,  //Some(F14),//pos ok
        JKey::Pause => None,       //Some(F15),//pos ok
        //JKey::Insert => Some(INSERT),
        JKey::Unknown(_) => None,
        JKey::Application => None,

        //JKey::Unknown(code) => code.try_into().ok(),
        _ => None,
    }
}
