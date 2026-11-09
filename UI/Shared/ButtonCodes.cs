namespace NativeEngine;

internal static class ButtonCodes
{
	//The Windows virtual key a button is, which key events carry; nought for one that has none
	internal static int ToVirtualKey( ButtonCode code )
	{
		if ( code >= ButtonCode.KEY_A && code <= ButtonCode.KEY_Z )
			return 'A' + (code - ButtonCode.KEY_A);

		if ( code >= ButtonCode.KEY_0 && code <= ButtonCode.KEY_9 )
			return '0' + (code - ButtonCode.KEY_0);

		if ( code >= ButtonCode.KEY_F1 && code <= ButtonCode.KEY_F12 )
			return 0x70 + (code - ButtonCode.KEY_F1);

		if ( code >= ButtonCode.KEY_F13 && code <= ButtonCode.KEY_F24 )
			return 0x7C + (code - ButtonCode.KEY_F13);

		if ( code >= ButtonCode.KEY_PAD_0 && code <= ButtonCode.KEY_PAD_9 )
			return 0x60 + (code - ButtonCode.KEY_PAD_0);

		return code switch
		{
			ButtonCode.KEY_BACKSPACE => 0x08,
			ButtonCode.KEY_TAB => 0x09,
			ButtonCode.KEY_ENTER => 0x0D,
			ButtonCode.KEY_LSHIFT or ButtonCode.KEY_RSHIFT => 0x10,
			ButtonCode.KEY_LCONTROL or ButtonCode.KEY_RCONTROL => 0x11,
			ButtonCode.KEY_LALT or ButtonCode.KEY_RALT => 0x12,
			ButtonCode.KEY_BREAK => 0x13,
			ButtonCode.KEY_CAPSLOCK => 0x14,
			ButtonCode.KEY_ESCAPE => 0x1B,
			ButtonCode.KEY_SPACE => 0x20,
			ButtonCode.KEY_PAGEUP => 0x21,
			ButtonCode.KEY_PAGEDOWN => 0x22,
			ButtonCode.KEY_END => 0x23,
			ButtonCode.KEY_HOME => 0x24,
			ButtonCode.KEY_LEFT => 0x25,
			ButtonCode.KEY_UP => 0x26,
			ButtonCode.KEY_RIGHT => 0x27,
			ButtonCode.KEY_DOWN => 0x28,
			ButtonCode.KEY_INSERT => 0x2D,
			ButtonCode.KEY_DELETE => 0x2E,
			ButtonCode.KEY_LWIN => 0x5B,
			ButtonCode.KEY_RWIN => 0x5C,
			ButtonCode.KEY_APP => 0x5D,
			ButtonCode.KEY_PAD_MULTIPLY => 0x6A,
			ButtonCode.KEY_PAD_PLUS => 0x6B,
			ButtonCode.KEY_PAD_MINUS => 0x6D,
			ButtonCode.KEY_PAD_DECIMAL => 0x6E,
			ButtonCode.KEY_PAD_DIVIDE => 0x6F,
			ButtonCode.KEY_PAD_ENTER => 0x0D,
			ButtonCode.KEY_NUMLOCK => 0x90,
			ButtonCode.KEY_SCROLLLOCK => 0x91,
			ButtonCode.KEY_PRINTSCREEN => 0x2C,
			ButtonCode.KEY_SEMICOLON => 0xBA,
			ButtonCode.KEY_EQUAL => 0xBB,
			ButtonCode.KEY_COMMA => 0xBC,
			ButtonCode.KEY_MINUS => 0xBD,
			ButtonCode.KEY_PERIOD => 0xBE,
			ButtonCode.KEY_SLASH => 0xBF,
			ButtonCode.KEY_BACKQUOTE => 0xC0,
			ButtonCode.KEY_LBRACKET => 0xDB,
			ButtonCode.KEY_BACKSLASH => 0xDC,
			ButtonCode.KEY_RBRACKET => 0xDD,
			ButtonCode.KEY_APOSTROPHE => 0xDE,
			ButtonCode.KEY_LESS => 0xE2,
			_ => 0,
		};
	}}
