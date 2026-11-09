using System.Collections.Concurrent;
using SkiaSharp;

namespace Rinten.UI.Native;

//A picture on the GL device. Made anywhere - pictures load on worker threads - and put on the device the first time it is drawn,
//which is on the main thread with the context current. Pictures are sRGB and decoded on sampling; letters are taken as written.
internal sealed unsafe class GlTexture : ITexture
{
	public int Width { get; }
	public int Height { get; }
	public bool IsValid { get; }
	public int Mips { get; }
	public int DirtyVersion { get; private set; }
	public int Content { get; private set; }
	public long DrawnAt { get; private set; }

	private readonly bool srgb, bgra;
	private byte[] pending;
	private uint name;
	private bool disposed;

	//Device names to free, from Dispose on whatever thread, freed by the next frame
	internal static readonly ConcurrentQueue<uint> Dead = new();

	public GlTexture( int width, int height, ReadOnlySpan<byte> data, bool srgb, bool bgra, int mips, bool valid = true )
	{
		Width = Math.Max( width, 1 );
		Height = Math.Max( height, 1 );
		IsValid = valid;
		this.srgb = srgb;
		this.bgra = bgra;
		Mips = mips == 0 ? (int)MathF.Log2( MathF.Max( Width, Height ) ) + 1 : Math.Max( mips, 1 );
		pending = data.IsEmpty ? new byte[Width * Height * 4] : data.ToArray();
	}

	public void MarkUsed() => DrawnAt = UiHost.Frame;

	public void Update( ReadOnlySpan<byte> data, int x = 0, int y = 0, int width = 0, int height = 0 )
	{
		if ( width == 0 || height == 0 ) { x = 0; y = 0; width = Width; height = Height; }

		lock ( this )
		{
			pending ??= new byte[Width * Height * 4];

			//The whole picture is sent again: the parts written are few and small, and one path is one thing to get right
			for ( var row = 0; row < height; row++ )
				data.Slice( row * width * 4, width * 4 ).CopyTo( pending.AsSpan( ((y + row) * Width + x) * 4, width * 4 ) );

			Content++;
		}
	}

	//The device's name for it, sending what was written since; nought for nothing to draw
	public uint Handle()
	{
		if ( !IsValid || disposed ) return 0;

		byte[] bytes;
		lock ( this ) { bytes = pending; pending = null; }

		if ( bytes is null ) return name;

		var created = name == 0;
		if ( created )
		{
			fixed ( uint* n = &name ) Gl.GenTextures( 1, n );
		}

		Gl.BindTexture( Gl.Texture2D, name );
		Gl.PixelStorei( Gl.UnpackAlignment, 4 );

		fixed ( byte* p = bytes )
		{
			if ( created )
				Gl.TexImage2D( Gl.Texture2D, 0, srgb ? Gl.Srgb8Alpha8 : Gl.Rgba8, Width, Height, 0, bgra ? Gl.Bgra : Gl.Rgba, Gl.UnsignedByte, p );
			else
				Gl.TexSubImage2D( Gl.Texture2D, 0, 0, 0, Width, Height, bgra ? Gl.Bgra : Gl.Rgba, Gl.UnsignedByte, p );
		}

		if ( created )
		{
			Gl.TexParameteri( Gl.Texture2D, Gl.TextureMinFilter, Mips > 1 ? Gl.LinearMipmapLinear : Gl.Linear );
			Gl.TexParameteri( Gl.Texture2D, Gl.TextureMagFilter, Gl.Linear );
			Gl.TexParameteri( Gl.Texture2D, Gl.TextureMaxLevel, Mips - 1 );

			//Repeating, as the engine's sampler does: tiled backgrounds need it, and the shader holds the rest to their edges
			Gl.TexParameteri( Gl.Texture2D, Gl.TextureWrapS, Gl.Repeat );
			Gl.TexParameteri( Gl.Texture2D, Gl.TextureWrapT, Gl.Repeat );
		}

		if ( Mips > 1 ) Gl.GenerateMipmap( Gl.Texture2D );

		return name;
	}

	public void Dispose()
	{
		if ( disposed ) return;
		disposed = true;
		pending = null;

		if ( name != 0 ) Dead.Enqueue( name );
		name = 0;
	}
}

//Where an app's pictures come from: made from bytes, or read from its files and decoded by Skia
internal sealed class GlTextureHost : ITextureHost
{
	public ITexture Invalid { get; } = new GlTexture( 1, 1, default, true, false, 1, valid: false );
	public ITexture Transparent { get; } = new GlTexture( 1, 1, new byte[4], true, false, 1 );

	public ITexture Create( in TextureRequest request )
		=> new GlTexture( request.Width, request.Height, request.Data, srgb: !request.Premultiplied, bgra: request.Format == TexturePixels.Bgra8, request.Mips );

	public ITexture Load( string path )
	{
		try
		{
			var bytes = Read( path );
			if ( bytes is null ) return Invalid;

			using var decoded = SKBitmap.Decode( bytes );
			if ( decoded is null ) return Invalid;

			using var rgba = new SKBitmap( new SKImageInfo( decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul ) );
			if ( !decoded.CopyTo( rgba, SKColorType.Rgba8888 ) ) return Invalid;

			return new GlTexture( rgba.Width, rgba.Height, rgba.GetPixelSpan(), srgb: true, bgra: false, mips: 0 );
		}
		catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException or ArgumentException )
		{
			Log.Warning( $"cannot read the picture {path}: {e.Message}" );
			return Invalid;
		}
	}

	public Task<ITexture> LoadAsync( string path ) => Task.Run( () => Load( path ) );

	//From the app's own files first, then from the disk as given
	private static byte[] Read( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) ) return null;

		var mount = UiContext.Current.FileMount;
		if ( mount is not null && mount.FileExists( path ) ) return mount.ReadAllBytes( path ).ToArray();

		return File.Exists( path ) ? File.ReadAllBytes( path ) : null;
	}
}
