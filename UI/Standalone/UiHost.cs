namespace Rinten.UI;

/// <summary>What an app's backend gives the toolkit: Rinten.UI.Native sets these when it opens its first window.</summary>
public static class UiHost
{
	/// <summary>Makes and loads the textures panels draw.</summary>
	public static ITextureHost Textures { get; set; }

	/// <summary>The device the batches go to, as far as the panel walk needs to know it.</summary>
	public static IUiDevice Device { get; set; } = new PlainDevice();

	/// <summary>Puts text on the system clipboard.</summary>
	public static Action<string> SetClipboard { get; set; } = _ => { };

	/// <summary>Reads text off the system clipboard.</summary>
	public static Func<string> GetClipboard { get; set; } = () => "";

	/// <summary>Plays a named interface sound.</summary>
	public static Action<string> PlaySound { get; set; } = _ => { };

	/// <summary>The desktop window the app runs in, for its own title bar; null where there is none. Rinten.UI.Native sets it.</summary>
	public static IAppWindow Window { get; set; }

	/// <summary>Reads a value kept under a key as JSON, null for none; kept for the run unless the app stores them somewhere.</summary>
	public static Func<string, string> LoadKept { get; set; } = key => kept.TryGetValue( key, out var json ) ? json : null;

	/// <summary>Keeps a value under a key as JSON - see LoadKept.</summary>
	public static Action<string, string> SaveKept { get; set; } = ( key, json ) => kept[key] = json;

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> kept = new();

	/// <summary>The screen root panels are laid out on when nothing lays them out itself.</summary>
	public static Rect Screen { get; set; } = new( 0, 0, 1280, 720 );

	/// <summary>Whether nothing is shown at all - a test.</summary>
	public static bool IsHeadless { get; set; }

	/// <summary>Counts frames; the backend moves it on once a frame.</summary>
	public static long Frame { get; private set; }

	/// <summary>Moves <see cref="Frame"/> on.</summary>
	public static void NextFrame() => Frame++;

	private sealed class PlainDevice : IUiDevice
	{
		public int BindlessSlots => 0;
		public int ResourceGeneration => 0;
		public uint TextureIndex( ulong handle ) => 0;
	}
}

/// <summary>What a title bar drawn by the app asks of its window.</summary>
public interface IAppWindow
{
	bool IsMaximized { get; }

	/// <summary>No system frame: the app draws the caption and says where it drags with SetDragAreas.</summary>
	bool Frameless { get; set; }

	void Minimize();
	void ToggleMaximized();
	void Close();
	void SetTitle( string title );

	/// <summary>The parts of the caption that move the window, in its pixels; buttons stay out of them.</summary>
	void SetDragAreas( params Rect[] areas );
}
