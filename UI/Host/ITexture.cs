namespace Rinten;

/// <summary>A picture the interface can draw: the engine's <c>Texture</c>, or whatever Rinten.UI.Native keeps on its device.</summary>
public interface ITexture : IDisposable
{
	/// <summary>Width in pixels.</summary>
	int Width { get; }

	/// <summary>Height in pixels.</summary>
	int Height { get; }

	/// <summary>Width and height in pixels.</summary>
	Vector2 Size => new( Width, Height );

	/// <summary>Whether it holds a picture; a failed load gives one that does not.</summary>
	bool IsValid { get; }

	/// <summary>How many levels of detail it has.</summary>
	int Mips => 1;

	/// <summary>Says it was drawn this frame, so a host that streams, pauses or ages out pictures keeps it.</summary>
	void MarkUsed() { }

	/// <summary>The <see cref="Rinten.UI.UiPlatform.Frame"/> it was last drawn in.</summary>
	long DrawnAt => 0;

	/// <summary>Moves when the picture is swapped for another, which may be another size.</summary>
	int DirtyVersion => 0;

	/// <summary>Moves whenever its pixels are written, so a kept frame can tell new pixels from old.</summary>
	int Content => 0;

	/// <summary>Whether every pixel is fully opaque, so what is under it need not be drawn.</summary>
	bool Opaque => false;

	/// <summary>Writes pixels, in the texture's own format, over a part of it; zero width or height means all of it.</summary>
	void Update( ReadOnlySpan<byte> data, int x = 0, int y = 0, int width = 0, int height = 0 );
}
