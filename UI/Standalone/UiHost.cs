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
