using System.Runtime.InteropServices;

namespace Rinten.UI.Native;

//The part of SDL 3 an app's window needs, as SDL_video.h, SDL_events.h and friends of 3.4 declare it
internal static unsafe partial class Sdl
{
	private const string Lib = "SDL3";

	public const uint InitVideo = 0x20;

	public const ulong WindowOpenGl = 0x2;
	public const ulong WindowHidden = 0x8;
	public const ulong WindowBorderless = 0x10;
	public const ulong WindowResizable = 0x20;
	public const ulong WindowMinimized = 0x40;
	public const ulong WindowMaximized = 0x80;
	public const ulong WindowHighPixelDensity = 0x2000;

	//SDL_GLAttr
	public const int GlRedSize = 0, GlGreenSize = 1, GlBlueSize = 2, GlAlphaSize = 3, GlDoubleBuffer = 5, GlDepthSize = 6, GlStencilSize = 7;
	public const int GlContextMajor = 17, GlContextMinor = 18, GlContextProfile = 20;
	public const int GlProfileCore = 1;

	//SDL_EventType
	public const uint EventQuit = 0x100;
	public const uint EventWindowShown = 0x202, EventWindowExposed = 0x204, EventWindowResized = 0x206, EventWindowPixelSizeChanged = 0x207;
	public const uint EventWindowMaximized = 0x20A, EventWindowRestored = 0x20B, EventWindowMouseLeave = 0x20D;
	public const uint EventWindowFocusLost = 0x20F, EventWindowCloseRequested = 0x210, EventWindowDisplayScaleChanged = 0x214;
	public const uint EventKeyDown = 0x300, EventKeyUp = 0x301, EventTextInput = 0x303;
	public const uint EventMouseMotion = 0x400, EventMouseButtonDown = 0x401, EventMouseButtonUp = 0x402, EventMouseWheel = 0x403;

	//SDL_Keymod
	public const ushort ModShift = 0x3, ModCtrl = 0xC0, ModAlt = 0x300;

	//SDL_HitTestResult
	public const int HitNormal = 0, HitDraggable = 1, HitResizeTopLeft = 2, HitResizeTop = 3, HitResizeTopRight = 4, HitResizeRight = 5;
	public const int HitResizeBottomRight = 6, HitResizeBottom = 7, HitResizeBottomLeft = 8, HitResizeLeft = 9;

	[StructLayout( LayoutKind.Sequential )]
	public struct Point
	{
		public int X, Y;
	}

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_Init( uint flags );

	[LibraryImport( Lib )]
	public static partial void SDL_Quit();

	[LibraryImport( Lib )]
	public static partial IntPtr SDL_GetError();

	public static string Error() => Marshal.PtrToStringUTF8( SDL_GetError() ) ?? "";

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetHint( string name, string value );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	public static partial IntPtr SDL_CreateWindow( string title, int w, int h, ulong flags );

	[LibraryImport( Lib )]
	public static partial void SDL_DestroyWindow( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_ShowWindow( IntPtr window );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetWindowTitle( IntPtr window, string title );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_GetWindowSizeInPixels( IntPtr window, out int w, out int h );

	[LibraryImport( Lib )]
	public static partial float SDL_GetWindowPixelDensity( IntPtr window );

	[LibraryImport( Lib )]
	public static partial float SDL_GetWindowDisplayScale( IntPtr window );

	[LibraryImport( Lib )]
	public static partial ulong SDL_GetWindowFlags( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_MinimizeWindow( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_MaximizeWindow( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_RestoreWindow( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetWindowBordered( IntPtr window, [MarshalAs( UnmanagedType.U1 )] bool bordered );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetWindowHitTest( IntPtr window, IntPtr callback, IntPtr data );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_GL_SetAttribute( int attr, int value );

	[LibraryImport( Lib )]
	public static partial IntPtr SDL_GL_CreateContext( IntPtr window );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_GL_DestroyContext( IntPtr context );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_GL_SetSwapInterval( int interval );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_GL_SwapWindow( IntPtr window );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	public static partial IntPtr SDL_GL_GetProcAddress( string name );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_PollEvent( byte* ev );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_WaitEventTimeout( byte* ev, int timeoutMs );

	[LibraryImport( Lib )]
	public static partial void SDL_PumpEvents();

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_StartTextInput( IntPtr window );

	[LibraryImport( Lib )]
	public static partial ushort SDL_GetModState();

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetClipboardText( string text );

	[LibraryImport( Lib )]
	public static partial IntPtr SDL_GetClipboardText();

	[LibraryImport( Lib )]
	public static partial void SDL_free( IntPtr memory );

	public static string ClipboardText()
	{
		var text = SDL_GetClipboardText();
		if ( text == IntPtr.Zero ) return "";

		try { return Marshal.PtrToStringUTF8( text ) ?? ""; }
		finally { SDL_free( text ); }
	}

	[LibraryImport( Lib )]
	public static partial IntPtr SDL_CreateSystemCursor( int id );

	[LibraryImport( Lib )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetCursor( IntPtr cursor );

	//File dialogs, through the properties API so they take a title
	[LibraryImport( Lib )]
	public static partial void SDL_ShowFileDialogWithProperties( int type, IntPtr callback, IntPtr userdata, uint props );

	[LibraryImport( Lib )]
	public static partial uint SDL_CreateProperties();

	[LibraryImport( Lib )]
	public static partial void SDL_DestroyProperties( uint props );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetStringProperty( uint props, string name, string value );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetPointerProperty( uint props, string name, IntPtr value );

	[LibraryImport( Lib, StringMarshalling = StringMarshalling.Utf8 )]
	[return: MarshalAs( UnmanagedType.U1 )]
	public static partial bool SDL_SetBooleanProperty( uint props, string name, [MarshalAs( UnmanagedType.U1 )] bool value );
}
