using System.Reflection;
using System.Runtime.InteropServices;
using static Rinten.CoreUiRenderer;

namespace Rinten.UI.Native;

//Draws a root panel's batches with the box shader. Into a picture of its own, sRGB so the shader's linear colours blend the way the
//engine's do, then copied onto the window as it stands: the window's own buffer may or may not encode, and this way it need not.
internal sealed unsafe class GlRenderer
{
	private uint program, vao, vbo, target, targetTexture;
	private int screenUniform, colorUniform;
	private int targetWidth, targetHeight;
	private nint vboBytes;
	private readonly List<UiBatch> batches = new();
	private readonly GlTexture white = new( 1, 1, new byte[] { 255, 255, 255, 255 }, true, false, 1 );

	public string Device { get; private set; }

	public void Init()
	{
		Device = $"{Gl.Text( Gl.Renderer )}, OpenGL {Gl.Text( Gl.Version )}";

		program = Link( Compile( Gl.VertexShader, Source( "ui.vert.glsl" ) ), Compile( Gl.FragmentShader, Source( "ui.frag.glsl" ) ) );
		screenUniform = Uniform( "u_screen" );
		colorUniform = Uniform( "u_color" );

		fixed ( uint* p = &vao ) Gl.GenVertexArrays( 1, p );
		fixed ( uint* p = &vbo ) Gl.GenBuffers( 1, p );

		Gl.BindVertexArray( vao );
		Gl.BindBuffer( Gl.ArrayBuffer, vbo );

		var stride = Marshal.SizeOf<UiVertex>();

		void F( uint location, int count, string field ) => Attribute( location, count, Gl.Float, false, stride, field );
		void U( uint location, int count, string field ) => Attribute( location, count, Gl.UnsignedInt, true, stride, field );

		F( 0, 2, nameof( UiVertex.Position ) );
		F( 1, 2, nameof( UiVertex.Uv ) );
		F( 2, 4, nameof( UiVertex.Rect ) );
		F( 3, 4, nameof( UiVertex.Radii ) );
		Attribute( 4, 4, Gl.UnsignedByte, false, stride, nameof( UiVertex.Fill ), normalised: true );
		U( 5, 4, nameof( UiVertex.BorderLeft ) );
		F( 6, 4, nameof( UiVertex.Misc ) );
		F( 7, 4, nameof( UiVertex.Clip ) );
		F( 8, 4, nameof( UiVertex.Filter ) );
		F( 9, 2, nameof( UiVertex.Filter2 ) );
		F( 10, 2, nameof( UiVertex.Local ) );
		U( 11, 1, nameof( UiVertex.Texture ) );
		F( 12, 4, nameof( UiVertex.ClipRadii ) );
		U( 13, 4, nameof( UiVertex.VerticalTop ) );
	}

	//The four border colours and the four vertical radii each sit side by side in the vertex, so one four-wide attribute reads each set
	private static void Attribute( uint location, int count, uint type, bool integer, int stride, string field, bool normalised = false )
	{
		var offset = Marshal.OffsetOf<UiVertex>( field );

		Gl.EnableVertexAttribArray( location );

		if ( integer ) Gl.VertexAttribIPointer( location, count, type, stride, offset );
		else Gl.VertexAttribPointer( location, count, type, normalised ? (byte)1 : (byte)0, stride, offset );
	}

	public void Draw( RootPanel root, int width, int height )
	{
		while ( GlTexture.Dead.TryDequeue( out var name ) ) Gl.DeleteTextures( 1, &name );

		if ( width <= 0 || height <= 0 ) return;

		Recycle( batches );
		Build( root, batches, NoText, Upload );

		Target( width, height );

		Gl.BindFramebuffer( Gl.Framebuffer, target );
		Gl.Viewport( 0, 0, width, height );
		Gl.Disable( Gl.DepthTest );
		Gl.Disable( Gl.CullFace );
		Gl.Disable( Gl.ScissorTest );
		Gl.Enable( Gl.FramebufferSrgb );
		Gl.ClearColor( 0, 0, 0, 1 );
		Gl.Clear( Gl.ColorBufferBit );

		//The shader writes straight alpha; what is under keeps its own coverage
		Gl.Enable( Gl.Blend );
		Gl.BlendFuncSeparate( Gl.SrcAlpha, Gl.OneMinusSrcAlpha, Gl.One, Gl.OneMinusSrcAlpha );

		Gl.UseProgram( program );
		Gl.Uniform4f( screenUniform, width, height, 0, 0 );
		Gl.Uniform1i( colorUniform, 0 );
		Gl.ActiveTexture( Gl.Texture0 );
		Gl.BindVertexArray( vao );
		Gl.BindBuffer( Gl.ArrayBuffer, vbo );

		var stride = Marshal.SizeOf<UiVertex>();

		foreach ( var batch in batches )
		{
			//The world is not here, and a backdrop would be a copy of this window to blur: an app has neither
			if ( batch.Texture == WorldTexture || batch.Texture == BackdropTexture ) continue;
			if ( batch.Vertices is null || batch.Vertices.Count == 0 ) continue;

			Gl.BindTexture( Gl.Texture2D, batch.Texture != 0 ? (uint)batch.Texture : white.Handle() );

			var vertices = CollectionsMarshal.AsSpan( batch.Vertices );
			var bytes = (nint)vertices.Length * stride;

			fixed ( UiVertex* data = vertices )
			{
				if ( bytes > vboBytes )
				{
					vboBytes = Math.Max( bytes, vboBytes * 2 );
					Gl.BufferData( Gl.ArrayBuffer, vboBytes, null, Gl.StreamDraw );
				}

				Gl.BufferSubData( Gl.ArrayBuffer, 0, bytes, data );
			}

			Gl.DrawArrays( Gl.Triangles, 0, vertices.Length );
		}

		if ( ShotPath is not null ) Shot( width, height );

		//Copied as the bytes stand: they are already encoded, and the window's buffer is not asked to encode them again
		Gl.Disable( Gl.FramebufferSrgb );
		Gl.BindFramebuffer( Gl.ReadFramebuffer, target );
		Gl.BindFramebuffer( Gl.DrawFramebuffer, 0 );
		Gl.BlitFramebuffer( 0, 0, width, height, 0, 0, width, height, Gl.ColorBufferBit, Gl.Nearest );
		Gl.BindFramebuffer( Gl.Framebuffer, 0 );
	}

	//RINTEN_UI_SHOT=file.png: the frame as drawn, written once and then the app closes - for looking at an app without a person at it
	public string ShotPath { get; set; }

	private void Shot( int width, int height )
	{
		var pixels = new byte[width * height * 4];
		fixed ( byte* p = pixels ) Gl.ReadPixels( 0, 0, width, height, Gl.Rgba, Gl.UnsignedByte, p );

		using var bitmap = new SkiaSharp.SKBitmap( new SkiaSharp.SKImageInfo( width, height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul ) );
		var row = width * 4;
		var target = bitmap.GetPixelSpan();

		//GL's rows run from the bottom
		fixed ( byte* dst = target )
			for ( var y = 0; y < height; y++ )
				pixels.AsSpan( (height - 1 - y) * row, row ).CopyTo( new Span<byte>( dst + y * row, row ) );

		using var file = File.Create( ShotPath );
		bitmap.Encode( file, SkiaSharp.SKEncodedImageFormat.Png, 100 );
		Log.Info( $"frame written to {ShotPath}" );

		ShotPath = null;
		App.Quit();
	}

	//The picture drawn into, remade when the window changes size
	private void Target( int width, int height )
	{
		if ( target != 0 && width == targetWidth && height == targetHeight ) return;

		if ( target != 0 )
		{
			fixed ( uint* p = &target ) Gl.DeleteFramebuffers( 1, p );
			fixed ( uint* p = &targetTexture ) Gl.DeleteTextures( 1, p );
		}

		targetWidth = width;
		targetHeight = height;

		fixed ( uint* p = &targetTexture ) Gl.GenTextures( 1, p );
		Gl.BindTexture( Gl.Texture2D, targetTexture );
		Gl.TexImage2D( Gl.Texture2D, 0, Gl.Srgb8Alpha8, width, height, 0, Gl.Rgba, Gl.UnsignedByte, null );
		Gl.TexParameteri( Gl.Texture2D, Gl.TextureMinFilter, Gl.Linear );
		Gl.TexParameteri( Gl.Texture2D, Gl.TextureMagFilter, Gl.Linear );

		fixed ( uint* p = &target ) Gl.GenFramebuffers( 1, p );
		Gl.BindFramebuffer( Gl.Framebuffer, target );
		Gl.FramebufferTexture2D( Gl.Framebuffer, Gl.ColorAttachment0, Gl.Texture2D, targetTexture, 0 );

		if ( Gl.CheckFramebufferStatus( Gl.Framebuffer ) != Gl.FramebufferComplete )
			throw new InvalidOperationException( "OpenGL cannot draw into an sRGB picture here" );
	}

	private static ulong Upload( ITexture texture ) => texture is GlTexture gl ? gl.Handle() : 0;

	//Labels draw their own pictures; a label with no text engine behind it - which an app's never lacks - draws nothing
	private static (ulong Texture, Vector2 Size) NoText( in TextRendering.Scope scope ) => (0, default);

	private static string Source( string name )
	{
		using var stream = typeof( GlRenderer ).Assembly.GetManifestResourceStream( name )
			?? throw new InvalidOperationException( $"the shader {name} is not in the assembly" );
		using var reader = new StreamReader( stream );
		return reader.ReadToEnd();
	}

	private static uint Compile( uint kind, string source )
	{
		var shader = Gl.CreateShader( kind );
		var bytes = System.Text.Encoding.UTF8.GetBytes( source );
		var length = bytes.Length;

		fixed ( byte* p = bytes )
		{
			var pp = p;
			Gl.ShaderSource( shader, 1, &pp, &length );
		}

		Gl.CompileShader( shader );

		int ok;
		Gl.GetShaderiv( shader, Gl.CompileStatus, &ok );
		if ( ok == 0 ) throw new InvalidOperationException( $"shader: {Info( shader, false )}" );

		return shader;
	}

	private static uint Link( uint vs, uint fs )
	{
		var p = Gl.CreateProgram();
		Gl.AttachShader( p, vs );
		Gl.AttachShader( p, fs );
		Gl.LinkProgram( p );

		int ok;
		Gl.GetProgramiv( p, Gl.LinkStatus, &ok );
		if ( ok == 0 ) throw new InvalidOperationException( $"shader link: {Info( p, true )}" );

		Gl.DeleteShader( vs );
		Gl.DeleteShader( fs );
		return p;
	}

	private static string Info( uint name, bool isProgram )
	{
		var log = new byte[4096];
		int written;

		fixed ( byte* p = log )
		{
			if ( isProgram ) Gl.GetProgramInfoLog( name, log.Length, &written, p );
			else Gl.GetShaderInfoLog( name, log.Length, &written, p );
		}

		return System.Text.Encoding.UTF8.GetString( log, 0, written );
	}

	private int Uniform( string name )
	{
		var bytes = System.Text.Encoding.ASCII.GetBytes( name + "\0" );
		fixed ( byte* p = bytes ) return Gl.GetUniformLocation( program, p );
	}
}
