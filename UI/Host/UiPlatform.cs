namespace Rinten.UI;

//What the toolkit asks of the program it is built into, answered at compile time: Rinten.Engine implements it, and an app gets Standalone/
internal static partial class UiPlatform
{
	//A panel for a razor or markup element name, by [Library], [Alias] or class name; null if no type answers to it
	internal static partial Panel CreatePanel( string elementName );

	//Sets a property by name, the way a razor attribute is set
	internal static partial void SetProperty( object target, string name, string value );

	//Whether the program's type library knows this type, so its panel events are looked up
	internal static partial bool Describes( Type type );

	//A stylesheet's text, made ready to parse - the engine puts its design tokens in front
	internal static partial string PrepareSheet( string text, string file );

	//The pointer, in screen pixels
	internal static partial Vector2 PointerPosition { get; set; }
	internal static partial Vector2 PointerDelta { get; }
	internal static partial Vector2 PointerVelocity { get; }
	internal static partial bool PointerVisible { get; }
	internal static partial void SetCursor( string name );

	//A gamepad standing in for the mouse: moves the pointer, and says whether it clicks and how it scrolls
	internal static partial void VirtualCursor( ref bool leftDown, ref Vector2 wheel );

	//Plays an interface sound from a panel, placed where the panel is
	internal static partial void PlaySound( Panel panel, string sound );

	//A #token's text in the player's language
	internal static partial string Phrase( string token );

	//A press somewhere: the host closes the popups that press was not in
	internal static partial void ClosePopups( Panel pressed );

	//Starts fetching a file that is not here yet, as a page does; false if it never will be
	internal static partial bool Fetch( string path, Action arrived );

	//Runs a loop's iterations on the host's workers, or one after another where it has none
	internal static partial void For<TState>( int count, TState state, Action<TState, int> body );
	internal static partial bool InOrder { get; }

	//Where textures come from and the device the walk builds batches for
	internal static partial ITextureHost Textures { get; }
	internal static partial IUiDevice Device { get; }

	//The frame counter kept pictures and kept walks are aged by, and the clock panels animate and wait by
	internal static partial long Frame();
	internal static partial double Now();
	internal static partial float TimeScale();

	//The screen the game's root panels are laid out on, asked once a frame
	internal static partial Rect Screen();

	//Runs the main thread's queued async work while something blocks on it; Spin keeps a tool window alive meanwhile
	internal static partial void PumpAsync();
	internal static partial void Spin();

	//After the panels took their input: the host routes keyboard and mouse between itself and the interface
	internal static partial void InputTicked( UISystem ui );

	//Panels in a world, pointed at by things in it rather than by the mouse
	internal static partial void TickWorldInput( UISystem ui );

	//The host's say over the pointer before any panel's: shown, hidden, or null for nothing to say
	internal static partial bool? PointerForced();

	//A root that does not count toward showing the pointer, whatever it wants
	internal static partial bool HidesPointer( RootPanel root );

	//A timed section in the host's profiler
	internal static partial UiScope Scope( string name );

	//Nothing is shown - a server, a test - and whether this runs in a browser, on one thread
	internal static partial bool IsHeadless { get; }
	internal static partial bool OnWeb { get; }

	//Puts text on the system clipboard
	internal static partial void SetClipboard( string text );
}

//A profiler section, ended by disposing it; what it holds is the host's
internal ref partial struct UiScope
{
}
