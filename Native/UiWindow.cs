using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeEngine;

namespace Rinten.UI.Native;

//All windows share one GL context, made current on each in turn; each has its own pointer, keys go to the focused panel
/// <summary>One of several windows of panels, run together by <see cref="RunAll"/>; <see cref="App"/> is for one window.</summary>
public sealed unsafe class UiWindow : IDisposable
{
	private static readonly List<UiWindow> open = new();
	private static IntPtr context;
	private static bool started, quit;

	private IntPtr handle;
	private readonly uint id;
	private readonly GlRenderer renderer = new();
	private readonly WindowInput input = new();
	private bool showWhenDrawn, everDrawn, covers;
	private Vector2 lastPointer;

	/// <summary>The windows open now, in the order they were opened.</summary>
	public static IReadOnlyList<UiWindow> AllOpen => open;

	/// <summary>The window with the keyboard, or null when it is some other program's.</summary>
	public static UiWindow Focused { get; private set; }

	/// <summary>The panels of this window. Laid out at the window's size, scaled the way its display is.</summary>
	public RootPanel Root { get; }

	/// <summary>What shows where no panel draws. <see cref="Color.Transparent"/> lets the desktop show through.</summary>
	public Color Background { get; set; } = Color.Black;

	/// <summary>The title it was opened with; window managers place windows by it.</summary>
	public string Title { get; }

	/// <summary>Whether it is still open: closing it from the window manager closes it here too.</summary>
	public bool IsOpen => handle != IntPtr.Zero;

	/// <summary>The window's size in pixels.</summary>
	public (int Width, int Height) Size
	{
		get
		{
			if ( !IsOpen ) return (0, 0);
			Sdl.SDL_GetWindowSizeInPixels( handle, out var w, out var h );
			return (w, h);
		}
	}

	/// <summary>Pixels per interface unit: the display's scale.</summary>
	public float Scale => IsOpen && Sdl.SDL_GetWindowDisplayScale( handle ) is var s && s > 0 ? s : 1.0f;

	//The window's units to its pixels: SDL reports the pointer in the first, the panels are laid out in the second
	private float Density => IsOpen && Sdl.SDL_GetWindowPixelDensity( handle ) is var d && d > 0 ? d : 1.0f;

	//Windows open hidden, so one appears with its first frame drawn rather than as an empty box
	/// <summary>Shown on the screen; set once the panels are built.</summary>
	public bool Shown
	{
		get => IsOpen && (Sdl.SDL_GetWindowFlags( handle ) & Sdl.WindowHidden) == 0;
		set
		{
			if ( !IsOpen ) return;

			if ( !value ) { showWhenDrawn = false; Sdl.SDL_HideWindow( handle ); return; }

			if ( everDrawn ) Sdl.SDL_ShowWindow( handle );
			else showWhenDrawn = true;

			Activity( 2.0 );
		}
	}

	private UiWindow( string title, IntPtr handle )
	{
		Title = title;
		this.handle = handle;
		id = Sdl.SDL_GetWindowID( handle );
		Root = new WindowRoot( this );
	}

	/// <summary>Opens a window, hidden until <see cref="Shown"/> and frameless unless bordered; null if none can be made.</summary>
	public static UiWindow Open( string title, int width, int height, bool bordered = false )
	{
		Start();

		var flags = Sdl.WindowOpenGl | Sdl.WindowHighPixelDensity | Sdl.WindowHidden | Sdl.WindowTransparent | Sdl.WindowResizable;
		if ( !bordered ) flags |= Sdl.WindowBorderless;

		var handle = Sdl.SDL_CreateWindow( title, width, height, flags );
		if ( handle == IntPtr.Zero )
		{
			Log.Warning( $"no window {title}: {Sdl.Error()}" );
			return null;
		}

		if ( context == IntPtr.Zero && !MakeContext( handle ) )
		{
			Sdl.SDL_DestroyWindow( handle );
			return null;
		}

		Sdl.SDL_GL_MakeCurrent( handle, context );

		var window = new UiWindow( title, handle );
		window.renderer.Init();
		open.Add( window );

		Activity( 2.0 );
		return window;
	}

	//Once, with the first window: everything an app's backend gives the toolkit
	private static void Start()
	{
		if ( started ) return;
		started = true;

		App.StartSdl();

		//Alpha must be asked for when the surface is made, or Wayland takes every window as opaque and corners turn black
		Sdl.SDL_GL_SetAttribute( Sdl.GlAlphaSize, 8 );

		App.StartHost();
	}

	private static bool MakeContext( IntPtr window )
	{
		context = Sdl.SDL_GL_CreateContext( window );
		if ( context == IntPtr.Zero )
		{
			Log.Warning( $"OpenGL 3.3: {Sdl.Error()}" );
			return false;
		}

		Gl.Load();

		//Each window presents in turn; waiting a refresh for each would make several windows several refreshes slow
		Sdl.SDL_GL_SetSwapInterval( 0 );
		return true;
	}

	/// <summary>The part of the primary display windows may use: less the panels of whatever else is on it.</summary>
	public static (int X, int Y, int Width, int Height) UsableDesktop()
	{
		Start();

		var display = Sdl.SDL_GetPrimaryDisplay();
		return display != 0 && Sdl.SDL_GetDisplayUsableBounds( display, out var b ) ? (b.X, b.Y, b.W, b.H) : (0, 0, 0, 0);
	}

	/// <summary>The whole primary display, the strips other panels reserve included.</summary>
	public static (int X, int Y, int Width, int Height) WholeDisplay()
	{
		Start();

		var display = Sdl.SDL_GetPrimaryDisplay();
		return display != 0 && Sdl.SDL_GetDisplayBounds( display, out var b ) ? (b.X, b.Y, b.W, b.H) : (0, 0, 0, 0);
	}

	/// <summary>Covers the whole display it is on, the strips other panels reserve included.</summary>
	public void CoverDisplay()
	{
		if ( !IsOpen ) return;

		//A hidden window is not yet anything to the window manager on Wayland, which fits it to the usable area when it maps:
		//it is asked again once it is shown
		covers = true;

		var display = Sdl.SDL_GetDisplayForWindow( handle );
		if ( display == 0 ) display = Sdl.SDL_GetPrimaryDisplay();
		if ( !Sdl.SDL_GetDisplayBounds( display, out var b ) ) return;

		Sdl.SDL_SetWindowPosition( handle, b.X, b.Y );
		Sdl.SDL_SetWindowSize( handle, b.W, b.H );
		Sdl.SDL_SyncWindow( handle );
	}

	/// <summary>Asks the window manager to bring it to the front with the keyboard.</summary>
	public void Raise()
	{
		if ( IsOpen ) Sdl.SDL_RaiseWindow( handle );
	}

	/// <summary>Closes the window. Its panels go with it.</summary>
	public void Dispose()
	{
		if ( !IsOpen ) return;

		open.Remove( this );
		if ( Focused == this ) Focused = null;

		Root.Delete( true );

		//The context outlives the window it was made with: it moves to another before this one goes
		Sdl.SDL_GL_MakeCurrent( open.Count > 0 ? open[0].handle : IntPtr.Zero, open.Count > 0 ? context : IntPtr.Zero );
		Sdl.SDL_DestroyWindow( handle );
		handle = IntPtr.Zero;

		Activity( 0.5 );
	}

	//Frames are drawn while something happens and for a moment after, so hovers and clicks settle; otherwise nothing at all.
	//A desktop that draws every frame like a game spends cores on a still picture.
	private static long activeUntil;

	private static void Activity( double seconds ) =>
		activeUntil = Math.Max( activeUntil, Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency) );

	/// <summary>Draws the windows now and for a moment after: something the panels show has changed.</summary>
	public static void Redraw() => Activity( 0.4 );

	/// <summary>Wakes <see cref="RunAll"/> from any thread, so it looks at the app's state now rather than at the next tick.</summary>
	public static void Wake()
	{
		var ev = stackalloc byte[128];
		new Span<byte>( ev, 128 ).Clear();
		*(uint*)ev = Sdl.EventUser;
		Sdl.SDL_PushEvent( ev );
	}

	/// <summary>Ends <see cref="RunAll"/> after the frame running now.</summary>
	public static void Quit() => quit = true;

	//Idle, the loop sleeps on the window system's events and wakes at least every idleMs to tick again
	/// <summary>Runs the windows until <see cref="Quit"/>; tick runs each turn on this thread and returns true when the picture changed.</summary>
	public static void RunAll( Func<bool> tick, int idleMs = 250 )
	{
		Start();

		var ui = UiContext.Current.UISystem;
		var ev = stackalloc byte[128];

		while ( !quit )
		{
			if ( tick() ) Redraw();

			var active = Stopwatch.GetTimestamp() < activeUntil;

			//Asleep until an event, rather than spinning over windows with nothing to draw
			if ( !active && Sdl.SDL_WaitEventTimeout( ev, idleMs ) ) Handle( ev, ui );

			while ( Sdl.SDL_PollEvent( ev ) ) Handle( ev, ui );

			if ( Stopwatch.GetTimestamp() < activeUntil ) Frame( ui );
		}

		foreach ( var window in open.ToArray() ) window.Dispose();
		ui.DeleteAllRoots();

		if ( context != IntPtr.Zero ) Sdl.SDL_GL_DestroyContext( context );
		Sdl.SDL_Quit();
	}

	private static readonly List<UiWindow> drawing = new();

	private static void Frame( UISystem ui )
	{
		UiHost.NextFrame();
		RealTime.Update( RealTime.NowDouble );

		//The keyboard, focus, the app's own async work and deferred deletions; window roots are left to their windows
		ui.Simulate( true );
		UiPlatform.PumpAsync();

		//A copy, kept between frames: a window's handlers may open or close windows while it draws
		drawing.Clear();
		drawing.AddRange( open );

		foreach ( var window in drawing )
		{
			if ( window.IsOpen ) window.Draw();
		}

		Input.Keyboard.EndFrame();
	}

	//The order the engine's windows keep: layout, then the pointer against it, then the handlers it queued, then drawing
	private void Draw()
	{
		var (width, height) = Size;
		if ( width <= 0 || height <= 0 ) return;

		UiHost.Screen = new Rect( 0, 0, width, height );

		try
		{
			Root.TickInternal();
			Root.PreLayout( new Rect( 0, 0, width, height ) );
			Root.CalculateLayout();
			Root.PostLayout();

			//Drags and tooltips read the pointer from the platform; it is this window's while this window runs
			UiPlatform.PointerPosition = input.State.MousePos;
			input.Tick( Root );
			Root.RunPendingEventsDeep();
		}
		catch ( Exception e )
		{
			Log.Error( e, $"window {Title}" );
		}

		if ( !IsOpen ) return;

		Sdl.SDL_GL_MakeCurrent( handle, context );
		renderer.Draw( Root, width, height, Background );
		Sdl.SDL_GL_SwapWindow( handle );

		everDrawn = true;

		if ( showWhenDrawn )
		{
			showWhenDrawn = false;
			Sdl.SDL_ShowWindow( handle );

			if ( covers ) CoverDisplay();
		}
	}

	private static UiWindow Of( byte* ev )
	{
		//Every window, key, mouse and text event has the window's id at the same place
		var windowId = *(uint*)(ev + 16);

		foreach ( var window in open )
			if ( window.id == windowId ) return window;

		return null;
	}

	private static void Handle( byte* ev, UISystem ui )
	{
		var type = *(uint*)ev;

		//What the loop itself is woken with; the tick that follows does the looking
		if ( type == Sdl.EventUser ) { Redraw(); return; }

		//Presenting a frame can ask for another; the compositor keeps the last one anyway
		if ( type == Sdl.EventWindowExposed ) return;

		Redraw();

		if ( type == Sdl.EventQuit ) return;

		var window = Of( ev );

		switch ( type )
		{
			case Sdl.EventWindowCloseRequested:
				window?.Dispose();
				break;

			case Sdl.EventWindowFocusGained:
				Focused = window;
				break;

			case Sdl.EventWindowFocusLost:
				if ( Focused == window ) Focused = null;
				Input.Keyboard.ReleaseAll( ui );
				break;

			case Sdl.EventWindowMouseLeave:
				if ( window is not null ) window.input.State.MousePos = new Vector2( -1, -1 );
				break;

			case Sdl.EventKeyDown:
			case Sdl.EventKeyUp:
			{
				var button = Keys.FromKeycode( *(uint*)(ev + 28) );
				if ( button == ButtonCode.BUTTON_CODE_INVALID ) button = Keys.FromScancode( *(uint*)(ev + 24) );
				if ( button == ButtonCode.BUTTON_CODE_INVALID ) break;

				var down = type == Sdl.EventKeyDown;
				var repeat = *(ev + 37) != 0;

				Input.Keyboard.Set( button, down, repeat );
				App.Key( ui, button, down, repeat, App.Modifiers( *(ushort*)(ev + 32) ) );
				break;
			}

			case Sdl.EventTextInput:
			{
				var text = Marshal.PtrToStringUTF8( *(IntPtr*)(ev + 24) );
				if ( string.IsNullOrEmpty( text ) ) break;

				foreach ( var letter in text )
					if ( !char.IsControl( letter ) ) ui.InputEventQueue.AddKeyTyped( letter );
				break;
			}

			case Sdl.EventMouseMotion:
			{
				if ( window is null ) break;

				var at = new Vector2( *(float*)(ev + 28), *(float*)(ev + 32) ) * window.Density;
				window.input.State.MousePos = at;
				window.lastPointer = at;
				break;
			}

			case Sdl.EventMouseButtonDown:
			case Sdl.EventMouseButtonUp:
			{
				var down = type == Sdl.EventMouseButtonDown;

				//SDL button 1 left, 2 middle, 3 right, 4 back, 5 forward
				var button = *(ev + 24) - 1;
				if ( button is < 0 or > 4 ) break;

				if ( down )
				{
					if ( window is null ) break;
					window.input.State.MousePos = new Vector2( *(float*)(ev + 28), *(float*)(ev + 32) ) * window.Density;
					window.input.Press( button );

					//A press in a window gives it the keyboard as far as this app can tell, before the window system says so
					Focused = window;
				}
				else
				{
					if ( window is not null ) window.input.State.MousePos = new Vector2( *(float*)(ev + 28), *(float*)(ev + 32) ) * window.Density;

					//There is one mouse: a release lets go of the button wherever it was pressed. Clicking a window can move the
					//window system's focus, and the release then comes marked with another window, leaving the press stuck
					foreach ( var w in open ) w.input.Release( button );
				}
				break;
			}

			case Sdl.EventMouseWheel:
			{
				if ( window is null ) break;

				var wheel = new Vector2( *(float*)(ev + 24), *(float*)(ev + 28) );
				if ( App.Modifiers( Sdl.SDL_GetModState() ).Contains( KeyboardModifiers.Shift ) ) wheel = new Vector2( -wheel.y, 0 );

				window.input.State.MouseWheel -= wheel;
				break;
			}
		}
	}

	//Laid out by its window, scaled by the display; the UI system's own loops keep off it
	private sealed class WindowRoot : RootPanel
	{
		private readonly UiWindow window;

		public WindowRoot( UiWindow window )
		{
			this.window = window;
			IsEditorUi = true;
		}

		public override bool IsWorldPanel => false;

		internal override bool ExternallyDriven => true;

		protected override void UpdateScale( Rect screenSize ) => Scale = window.Scale;

		private bool handingOn;

		//Keys nothing focused took come here and go down to the panels; one that bubbles back up stops here
		public override void OnButtonTyped( ButtonEvent e )
		{
			if ( handingOn ) return;

			handingOn = true;

			try
			{
				foreach ( var child in Children )
				{
					child.OnButtonTyped( e );
					if ( e.StopPropagation ) break;
				}
			}
			finally
			{
				handingOn = false;
			}
		}
	}

	//Hover, press and click against one window's panels, from the pointer that window's events say
	private sealed class WindowInput : PanelInput
	{
		public InputData State = new() { MousePos = new Vector2( -1, -1 ) };

		private readonly RootPanel[] justTheRoot = new RootPanel[1];
		private Vector2 movedFrom = new( float.NaN, float.NaN );
		private bool leftWasDown;
		private long lastPressed;
		private Vector2 lastPressedAt;

		//Presses the panels have not seen yet, and releases waiting for them to be seen: a quick click (down and up between
		//two frames) still reaches the panels as a press in one frame and a release in the next
		private int unseenPress, pendingRelease;

		public void Press( int button )
		{
			Set( button, true );
			unseenPress |= 1 << button;
			pendingRelease &= ~(1 << button);
		}

		public void Release( int button )
		{
			if ( (unseenPress & (1 << button)) != 0 ) pendingRelease |= 1 << button;
			else Set( button, false );
		}

		private void Set( int button, bool down )
		{
			switch ( button )
			{
				case 0: State.Mouse0 = down; break;
				case 1: State.Mouse1 = down; break; //PanelInput order: 1 is middle, 2 is right
				case 2: State.Mouse2 = down; break;
				case 3: State.Mouse3 = down; break;
				case 4: State.Mouse4 = down; break;
			}
		}

		public void Tick( RootPanel root )
		{
			if ( root is null || !root.IsValid ) return;

			root.TickInputInternal();

			justTheRoot[0] = root;
			Tick( justTheRoot, mouseIsActive: true );

			//Seen now; a release that came early goes through for the next frame
			unseenPress = 0;
			for ( var b = 0; b < 5; b++ )
				if ( (pendingRelease & (1 << b)) != 0 ) Set( b, false );
			pendingRelease = 0;

			Moved();
			DoubleClicked();

			UpdateTextInput( root );
		}

		//The wheel is how far it turned since the last frame: read once and gone
		internal override InputData GetInputData()
		{
			var data = State;
			State.MouseWheel = default;
			return data;
		}

		public override void SetCursor( string name ) => App.Cursor( name );

		private void Moved()
		{
			if ( State.MousePos == movedFrom ) return;
			movedFrom = State.MousePos;

			var target = Active ?? Hovered;
			target?.CreateEvent( new MousePanelEvent( "onmousemove", target, "none" ) );
		}

		//Two presses close in time and place; a third starts counting again rather than being a second double click
		private void DoubleClicked()
		{
			if ( State.Mouse0 == leftWasDown ) return;

			leftWasDown = State.Mouse0;
			if ( !leftWasDown ) return;

			var now = Stopwatch.GetTimestamp();
			var since = (now - lastPressed) / (double)Stopwatch.Frequency;
			var again = lastPressed != 0 && since <= 0.4 && (State.MousePos - lastPressedAt).Length <= 6.0f;

			lastPressed = again ? 0 : now;
			lastPressedAt = State.MousePos;

			if ( again && Hovered is { } target ) target.CreateEvent( new MousePanelEvent( "ondoubleclick", target, "mouseleft" ) );
		}

		//Typed text is asked for only while a field of this window has the focus: left on, a Wayland compositor
		//swallows printable keys into its input method and shortcuts never arrive as keys
		private bool texting;

		private void UpdateTextInput( RootPanel root )
		{
			var focused = InputFocus.Current;
			var wants = false;

			for ( var panel = focused; panel is not null; panel = panel.Parent )
			{
				if ( panel.AcceptsImeInput ) { wants = focused.FindRootPanel() == root; break; }
			}

			if ( wants == texting ) return;
			texting = wants;

			var window = open.FirstOrDefault( w => w.Root == root );
			if ( window is null ) return;

			if ( wants ) Sdl.SDL_StartTextInput( window.handle );
			else Sdl.SDL_StopTextInput( window.handle );
		}
	}
}
