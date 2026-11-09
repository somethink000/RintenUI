namespace Rinten.UI;

/// <summary>How the bytes of a new texture are laid out.</summary>
public enum TexturePixels
{
	/// <summary>Red, green, blue, alpha; a byte each.</summary>
	Rgba8,

	/// <summary>Blue, green, red, alpha; a byte each - what Skia paints on most machines.</summary>
	Bgra8,
}

/// <summary>A texture to make: its size, its pixels and how it is used.</summary>
public ref struct TextureRequest
{
	/// <summary>Width in pixels.</summary>
	public int Width;

	/// <summary>Height in pixels.</summary>
	public int Height;

	/// <summary>The layout of <see cref="Data"/>.</summary>
	public TexturePixels Format;

	/// <summary>The pixels, row after row; empty for a texture to be written later.</summary>
	public ReadOnlySpan<byte> Data;

	/// <summary>Levels of detail: 1 for none, 0 for a full chain.</summary>
	public int Mips;

	/// <summary>Written again often, as text is.</summary>
	public bool Dynamic;

	/// <summary>The colours are already multiplied by their alpha and are to be taken as they are.</summary>
	public bool Premultiplied;

	/// <summary>A name for debugging.</summary>
	public string Name;
}

/// <summary>Where textures come from: the host makes them on its device and loads them from its files.</summary>
public interface ITextureHost
{
	/// <summary>A texture of the given pixels.</summary>
	ITexture Create( in TextureRequest request );

	/// <summary>A picture from a file or url, or an invalid one if it cannot be read.</summary>
	ITexture Load( string path );

	/// <summary>The same, without waiting on the main thread.</summary>
	Task<ITexture> LoadAsync( string path );

	/// <summary>What stands in for a picture that could not be had.</summary>
	ITexture Invalid { get; }

	/// <summary>A texture with nothing in it.</summary>
	ITexture Transparent { get; }
}
