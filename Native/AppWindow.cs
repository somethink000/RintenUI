using System.Runtime.InteropServices;
using System.Threading;

namespace Rinten.UI.Native;

/// <summary>The app's window, as the engine's AppWindow puts it: its frame, its title, and the system's file boxes.</summary>
public static unsafe class AppWindow
{
	/// <summary>Whether there is a window at all.</summary>
	public static bool HasWindow => App.Window != IntPtr.Zero;

	/// <summary>Something is under way: the window keeps drawing every frame even if <see cref="SleepWhenIdle"/>.</summary>
	public static bool Busy { get; set; }

	/// <summary>Draws a few frames a second while nothing happens, rather than every frame.</summary>
	public static bool SleepWhenIdle { get; set; }

	/// <summary>Whether the window fills its screen.</summary>
	public static bool IsMaximized => HasWindow && (Sdl.SDL_GetWindowFlags( App.Window ) & Sdl.WindowMaximized) != 0;

	/// <summary>Folds the window away to the taskbar.</summary>
	public static void Minimize() { if ( HasWindow ) Sdl.SDL_MinimizeWindow( App.Window ); }

	/// <summary>Maximizes the window, or gives it back its size if it already is.</summary>
	public static void ToggleMaximized()
	{
		if ( !HasWindow ) return;

		if ( IsMaximized ) Sdl.SDL_RestoreWindow( App.Window );
		else Sdl.SDL_MaximizeWindow( App.Window );
	}

	/// <summary>What the taskbar calls the window.</summary>
	public static void SetTitle( string title ) { if ( HasWindow ) Sdl.SDL_SetWindowTitle( App.Window, title ?? "" ); }

	/// <summary>Closes the app.</summary>
	public static void Close() => App.Quit();

	//── frameless ──────────────────────────────────────────────────────────

	private static bool frameless;
	private static Rect[] dragAreas = [];

	/// <summary>Without the system's frame: the app draws its own caption and says where it drags with <see cref="SetDragAreas"/>.</summary>
	public static bool Frameless
	{
		get => frameless;
		set
		{
			if ( frameless == value || !HasWindow ) return;
			frameless = value;

			Sdl.SDL_SetWindowBordered( App.Window, !value );
			Sdl.SDL_SetWindowHitTest( App.Window, value ? (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, Sdl.Point*, IntPtr, int>)&HitTest : IntPtr.Zero, IntPtr.Zero );
		}
	}

	/// <summary>The parts of a frameless window's caption that drag it, in pixels; buttons stay out of them.</summary>
	public static void SetDragAreas( params Rect[] areas ) => dragAreas = (areas ?? []).Where( a => a.Width > 0 && a.Height > 0 ).ToArray();

	//How near an edge a press resizes rather than presses, in pixels
	private const int ResizeBorder = 5;

	[UnmanagedCallersOnly( CallConvs = new[] { typeof( System.Runtime.CompilerServices.CallConvCdecl ) } )]
	private static int HitTest( IntPtr window, Sdl.Point* area, IntPtr data )
	{
		//Called from inside SDL: nothing may throw back out through it
		try
		{
			if ( area == null ) return Sdl.HitNormal;

			var density = Sdl.SDL_GetWindowPixelDensity( window );
			if ( density <= 0 ) density = 1;

			var x = area->X * density;
			var y = area->Y * density;
			var (w, h) = App.Size;

			if ( !IsMaximized )
			{
				var left = x < ResizeBorder;
				var right = x >= w - ResizeBorder;
				var top = y < ResizeBorder;
				var bottom = y >= h - ResizeBorder;

				if ( top && left ) return Sdl.HitResizeTopLeft;
				if ( top && right ) return Sdl.HitResizeTopRight;
				if ( bottom && left ) return Sdl.HitResizeBottomLeft;
				if ( bottom && right ) return Sdl.HitResizeBottomRight;
				if ( top ) return Sdl.HitResizeTop;
				if ( bottom ) return Sdl.HitResizeBottom;
				if ( left ) return Sdl.HitResizeLeft;
				if ( right ) return Sdl.HitResizeRight;
			}

			foreach ( var a in dragAreas )
				if ( x >= a.Left && x < a.Right && y >= a.Top && y < a.Bottom ) return Sdl.HitDraggable;

			return Sdl.HitNormal;
		}
		catch
		{
			return Sdl.HitNormal;
		}
	}

	//── file boxes ─────────────────────────────────────────────────────────

	/// <summary>Asks for a folder in the system's own dialog; null if it was dismissed. Blocks until answered.</summary>
	public static string PickFolder( string title = null, string startIn = null ) => Show( 2, title, startIn, false )?.FirstOrDefault();

	/// <summary>Asks for files to open in the system's own dialog; null if it was dismissed. Blocks until answered.</summary>
	public static List<string> PickFiles( string title = null, string startIn = null, bool many = false ) => Show( 0, title, startIn, many );

	private delegate void Answered( IntPtr userdata, IntPtr files, int filter );

	//SDL_FileDialogType: 0 open, 1 save, 2 folder. The box answers on a callback; events are pumped until it has
	private static List<string> Show( int type, string title, string location, bool many )
	{
		List<string> answer = null;
		var finished = false;

		Answered got = ( _, files, _ ) =>
		{
			finished = true;
			if ( files == IntPtr.Zero ) return;

			var chosen = new List<string>();

			for ( var i = 0; ; i++ )
			{
				var p = Marshal.ReadIntPtr( files, i * IntPtr.Size );
				if ( p == IntPtr.Zero ) break;

				var path = Marshal.PtrToStringUTF8( p );
				if ( !string.IsNullOrEmpty( path ) ) chosen.Add( path );
			}

			if ( chosen.Count > 0 ) answer = chosen;
		};

		var handle = GCHandle.Alloc( got );
		var props = Sdl.SDL_CreateProperties();

		try
		{
			if ( HasWindow ) Sdl.SDL_SetPointerProperty( props, "SDL.filedialog.window", App.Window );
			if ( !string.IsNullOrEmpty( title ) ) Sdl.SDL_SetStringProperty( props, "SDL.filedialog.title", title );
			if ( !string.IsNullOrEmpty( location ) ) Sdl.SDL_SetStringProperty( props, "SDL.filedialog.location", location );
			if ( many ) Sdl.SDL_SetBooleanProperty( props, "SDL.filedialog.many", true );

			Sdl.SDL_ShowFileDialogWithProperties( type, Marshal.GetFunctionPointerForDelegate( got ), IntPtr.Zero, props );

			//Two minutes is longer than anybody looks for a file; a wait with no end would hang the app if the platform never answered
			for ( var waited = 0; !finished && waited < 120_000; waited += 10 )
			{
				Sdl.SDL_PumpEvents();
				Thread.Sleep( 10 );
			}
		}
		finally
		{
			handle.Free();
			if ( props != 0 ) Sdl.SDL_DestroyProperties( props );
		}

		return answer;
	}
}
