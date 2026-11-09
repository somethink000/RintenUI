using System.Runtime.InteropServices;
using NativeEngine;

namespace Rinten.UI.Native;

/// <summary>One window and the root panel in it: opens the window, runs the frames, closes when the window is closed.</summary>
public static unsafe class App
{
	internal static IntPtr Window;
	private static IntPtr context;
	private static bool quit;
	private static readonly GlRenderer renderer = new();

	/// <summary>How many pixels the interface takes for one of its units: the display's scale, which follows the system's setting.</summary>
	public static float Scale { get; private set; } = 1.0f;

	/// <summary>The window's size in pixels.</summary>
	public static (int Width, int Height) Size { get; private set; }

	/// <summary>Opens the window, makes the root panel and runs until the window is closed. Call it from Main, on the thread that will be the main one.</summary>
	public static void Run( string title, Func<RootPanel> makeRoot, int width = 1280, int height = 800 )
	{
		Open( title, width, height );

		var ui = UiContext.Current.UISystem;
		var root = makeRoot();

		Sdl.SDL_ShowWindow( Window );
		Sdl.SDL_StartTextInput( Window );

		var ev = stackalloc byte[128];
		var lastInput = RealTime.NowDouble;

		while ( !quit )
		{
			//Asleep between events when nothing moves: an idle window draws a few frames a second, any input wakes it for a while
			var idle = AppWindow.SleepWhenIdle && !AppWindow.Busy && RealTime.NowDouble - lastInput > 1.0;

			if ( idle && Sdl.SDL_WaitEventTimeout( ev, 250 ) )
			{
				Handle( ev, ui );
				lastInput = RealTime.NowDouble;
			}

			while ( Sdl.SDL_PollEvent( ev ) )
			{
				Handle( ev, ui );
				lastInput = RealTime.NowDouble;
			}

			Frame( ui, root );
			Input.Keyboard.EndFrame();
		}

		root.Delete( true );
		ui.DeleteAllRoots();

		Sdl.SDL_GL_DestroyContext( context );
		Sdl.SDL_DestroyWindow( Window );
		Sdl.SDL_Quit();
	}

	/// <summary>Ends the frame loop; the window closes after the frame running now.</summary>
	public static void Quit() => quit = true;

	private static void Open( string title, int width, int height )
	{
		ThreadSafe.MarkMainThread();

		if ( !Sdl.SDL_Init( Sdl.InitVideo ) ) throw new InvalidOperationException( $"SDL: {Sdl.Error()}" );

		Sdl.SDL_GL_SetAttribute( Sdl.GlContextMajor, 3 );
		Sdl.SDL_GL_SetAttribute( Sdl.GlContextMinor, 3 );
		Sdl.SDL_GL_SetAttribute( Sdl.GlContextProfile, Sdl.GlProfileCore );
		Sdl.SDL_GL_SetAttribute( Sdl.GlDoubleBuffer, 1 );
		Sdl.SDL_GL_SetAttribute( Sdl.GlDepthSize, 0 );
		Sdl.SDL_GL_SetAttribute( Sdl.GlStencilSize, 0 );

		Window = Sdl.SDL_CreateWindow( title, width, height,
			Sdl.WindowOpenGl | Sdl.WindowResizable | Sdl.WindowHighPixelDensity | Sdl.WindowHidden );
		if ( Window == IntPtr.Zero ) throw new InvalidOperationException( $"SDL window: {Sdl.Error()}" );

		context = Sdl.SDL_GL_CreateContext( Window );
		if ( context == IntPtr.Zero ) throw new InvalidOperationException( $"OpenGL 3.3: {Sdl.Error()}" );

		Gl.Load();
		Sdl.SDL_GL_SetSwapInterval( 1 );
		renderer.Init();
		Log.Info( $"window: {renderer.Device}" );

		Rinten.Tasks.SyncContext.Init();

		UiHost.Textures = new GlTextureHost();
		UiHost.SetClipboard = text => Sdl.SDL_SetClipboardText( text ?? "" );
		UiHost.GetClipboard = Sdl.ClipboardText;

		FontManager.Instance.LoadAll( UiContext.Current.FileMount );

		Measure();
	}

	//RINTEN_UI_SHOT=file.png writes a frame and closes, RINTEN_UI_SHOT_SECONDS after the start (2 by default): layout, fonts and pictures settle first
	private static readonly double shotAt = Environment.GetEnvironmentVariable( "RINTEN_UI_SHOT" ) is { Length: > 0 }
		? double.TryParse( Environment.GetEnvironmentVariable( "RINTEN_UI_SHOT_SECONDS" ), System.Globalization.CultureInfo.InvariantCulture, out var seconds ) ? seconds : 2.0
		: -1;

	private static bool shotTaken;

	private static void Measure()
	{
		Sdl.SDL_GetWindowSizeInPixels( Window, out var w, out var h );
		Size = (w, h);

		var scale = Sdl.SDL_GetWindowDisplayScale( Window );
		Scale = scale > 0 ? scale : 1.0f;

		UiHost.Screen = new Rect( 0, 0, w, h );
	}

	private static void Frame( UISystem ui, RootPanel root )
	{
		UiHost.NextFrame();
		RealTime.Update( RealTime.NowDouble );

		Measure();

		ui.Simulate( true );
		UiPlatform.PumpAsync();

		Cursor( Pointer.Cursor );

		if ( shotAt >= 0 && !shotTaken && RealTime.NowDouble >= shotAt )
		{
			shotTaken = true;
			renderer.ShotPath = Environment.GetEnvironmentVariable( "RINTEN_UI_SHOT" );
		}

		renderer.Draw( root, Size.Width, Size.Height );
		Sdl.SDL_GL_SwapWindow( Window );
	}

	//── input ──────────────────────────────────────────────────────────────

	private static Vector2 lastPointer;
	private static Vector2 lastClickAt;
	private static double lastClickTime;
	private static int clicks;

	private static KeyboardModifiers Modifiers( ushort mod )
	{
		var m = KeyboardModifiers.None;
		if ( (mod & Sdl.ModShift) != 0 ) m |= KeyboardModifiers.Shift;
		if ( (mod & Sdl.ModCtrl) != 0 ) m |= KeyboardModifiers.Ctrl;
		if ( (mod & Sdl.ModAlt) != 0 ) m |= KeyboardModifiers.Alt;
		return m;
	}

	//The window's units to its pixels: SDL reports the pointer in the first, the panels are laid out in the second
	private static float Density => Sdl.SDL_GetWindowPixelDensity( Window ) is var d && d > 0 ? d : 1.0f;

	//The way the engine routes a window's events to its interface (Rinten.Engine InputContext), less the game it shares them with
	private static void Handle( byte* ev, UISystem ui )
	{
		var type = *(uint*)ev;

		switch ( type )
		{
			case Sdl.EventQuit:
			case Sdl.EventWindowCloseRequested:
				quit = true;
				break;

			case Sdl.EventKeyDown:
			case Sdl.EventKeyUp:
			{
				var button = Keys.FromKeycode( *(uint*)(ev + 28) );
				if ( button == ButtonCode.BUTTON_CODE_INVALID ) button = Keys.FromScancode( *(uint*)(ev + 24) );
				if ( button == ButtonCode.BUTTON_CODE_INVALID ) break;

				var modifiers = Modifiers( *(ushort*)(ev + 32) );
				var down = type == Sdl.EventKeyDown;
				var repeat = *(ev + 37) != 0;

				Input.Keyboard.Set( button, down, repeat );
				Key( ui, button, down, repeat, modifiers );
				break;
			}

			case Sdl.EventTextInput:
			{
				var text = Marshal.PtrToStringUTF8( *(IntPtr*)(ev + 24) );
				if ( string.IsNullOrEmpty( text ) ) break;

				foreach ( var letter in text ) ui.InputEventQueue.AddKeyTyped( letter );
				break;
			}

			case Sdl.EventMouseMotion:
			{
				var at = new Vector2( *(float*)(ev + 28), *(float*)(ev + 32) ) * Density;
				Pointer.Delta = at - lastPointer;
				lastPointer = at;
				UiPlatform.PointerPosition = at;
				ui.InputEventQueue.MouseMoved( Pointer.Delta );
				break;
			}

			case Sdl.EventMouseButtonDown:
			case Sdl.EventMouseButtonUp:
			{
				var button = Keys.FromMouseButton( *(ev + 24) );
				if ( button == ButtonCode.BUTTON_CODE_INVALID ) break;

				var down = type == Sdl.EventMouseButtonDown;
				var at = new Vector2( *(float*)(ev + 28), *(float*)(ev + 32) ) * Density;
				UiPlatform.PointerPosition = at;

				MouseButton( ui, button, down, Modifiers( Sdl.SDL_GetModState() ) );
				break;
			}

			case Sdl.EventMouseWheel:
				ui.Input.AddMouseWheel( new Vector2( *(float*)(ev + 24), *(float*)(ev + 28) ), Modifiers( Sdl.SDL_GetModState() ) );
				break;

			case Sdl.EventWindowMouseLeave:
				UiPlatform.PointerPosition = new Vector2( -1, -1 );
				break;

			case Sdl.EventWindowFocusLost:
				Input.Keyboard.ReleaseAll( ui );
				break;
		}
	}

	private static void Key( UISystem ui, ButtonCode button, bool down, bool repeat, KeyboardModifiers modifiers )
	{
		if ( down ) ui.InputEventQueue.AddButtonTyped( button, modifiers );

		if ( down && button == ButtonCode.KEY_ESCAPE ) ui.CurrentFocus?.CreateEvent( "onescape" );

		if ( repeat ) return;

		//Ctrl alone with C, V or X is the clipboard, said to the focused panel as its own event
		if ( down && modifiers == KeyboardModifiers.Ctrl )
		{
			if ( button == ButtonCode.KEY_C ) { ui.InputEventQueue.QueueInputEvent( new CopyEvent() ); return; }
			if ( button == ButtonCode.KEY_X ) { ui.InputEventQueue.QueueInputEvent( new CutEvent() ); return; }

			if ( button == ButtonCode.KEY_V )
			{
				var text = Sdl.ClipboardText();
				if ( text.Length > 0 ) ui.InputEventQueue.QueueInputEvent( new PasteEvent( text ) );
				return;
			}
		}

		ui.InputEventQueue.AddButtonEvent( button, down, modifiers );
	}

	private static void MouseButton( UISystem ui, ButtonCode button, bool down, KeyboardModifiers modifiers )
	{
		//Two releases close together in time and place are a double click
		var at = UiPlatform.PointerPosition;

		if ( RealTime.NowDouble - lastClickTime > 0.4 || (at - lastClickAt).Length > 5.0f ) clicks = 0;
		if ( !down ) clicks++;

		lastClickAt = at;
		lastClickTime = RealTime.NowDouble;

		if ( !down && clicks == 2 ) ui.InputEventQueue.AddDoubleClick( button.ToString() );

		if ( down ) ui.InputEventQueue.AddButtonTyped( button, modifiers );
		ui.Input.AddMouseButton( button, down, modifiers );
	}

	//── cursor ─────────────────────────────────────────────────────────────

	private static string cursorShown = "";
	private static readonly Dictionary<int, IntPtr> cursors = new();

	private static void Cursor( string name )
	{
		name ??= "";
		if ( name == cursorShown ) return;
		cursorShown = name;

		//SDL_SystemCursor
		var id = name switch
		{
			"text" => 1,
			"wait" => 2,
			"crosshair" => 3,
			"progress" => 4,
			"nwse-resize" => 5,
			"nesw-resize" => 6,
			"ew-resize" or "col-resize" or "e-resize" or "w-resize" => 7,
			"ns-resize" or "row-resize" or "n-resize" or "s-resize" => 8,
			"move" or "grab" or "grabbing" or "all-scroll" => 9,
			"not-allowed" or "no-drop" => 10,
			"pointer" => 11,
			_ => 0,
		};

		if ( !cursors.TryGetValue( id, out var cursor ) ) cursors[id] = cursor = Sdl.SDL_CreateSystemCursor( id );
		if ( cursor != IntPtr.Zero ) Sdl.SDL_SetCursor( cursor );
	}
}

/// <summary>A root panel the size of the window, scaled the way the display is, with the lifecycle a component has in the engine.</summary>
public class AppRoot : RootPanel
{
	private bool started;

	protected override void UpdateScale( Rect screenSize ) => Scale = App.Scale;

	/// <summary>Once, on the first frame, before the first <see cref="OnUpdate"/>.</summary>
	protected virtual void OnStart() { }

	/// <summary>Every frame, before input and layout.</summary>
	protected virtual void OnUpdate() { }

	/// <summary>When the window closes.</summary>
	protected virtual void OnDestroy() { }

	public override void Tick()
	{
		base.Tick();

		if ( !started )
		{
			started = true;
			OnStart();
		}

		OnUpdate();
	}

	public override void OnDeleted()
	{
		OnDestroy();
		base.OnDeleted();
	}
}

/// <summary>Which keys are held, by the names binds use ("lctrl", "o", "enter").</summary>
public static class Input
{
	public static class Keyboard
	{
		private static readonly HashSet<ButtonCode> held = new();
		private static readonly HashSet<ButtonCode> pressed = new();

		/// <summary>Whether the key is held down now.</summary>
		public static bool Down( string name ) => held.Contains( Keys.StringToButtonCode( name ) );

		/// <summary>Whether the key went down this frame.</summary>
		public static bool Pressed( string name ) => pressed.Contains( Keys.StringToButtonCode( name ) );

		internal static void Set( ButtonCode button, bool down, bool repeat )
		{
			if ( down )
			{
				if ( !repeat ) pressed.Add( button );
				held.Add( button );
			}
			else held.Remove( button );
		}

		internal static void EndFrame() => pressed.Clear();

		//The window lost the keyboard: what was held is let go, and said to the panels so nothing stays pressed
		internal static void ReleaseAll( UISystem ui )
		{
			foreach ( var button in held ) ui.InputEventQueue.AddButtonEvent( button, false, KeyboardModifiers.None );
			held.Clear();
		}
	}
}
