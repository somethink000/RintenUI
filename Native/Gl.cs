using System.Runtime.InteropServices;

namespace Rinten.UI.Native;

//The OpenGL 3.3 core functions the panel renderer calls, loaded through SDL once a context is current
internal static unsafe class Gl
{
	public const uint ColorBufferBit = 0x4000;
	public const uint Blend = 0x0BE2, ScissorTest = 0x0C11, DepthTest = 0x0B71, CullFace = 0x0B44, FramebufferSrgb = 0x8DB9;
	public const uint SrcAlpha = 0x0302, OneMinusSrcAlpha = 0x0303, One = 1;
	public const uint Triangles = 0x0004;
	public const uint Float = 0x1406, UnsignedByte = 0x1401, UnsignedInt = 0x1405;
	public const uint ArrayBuffer = 0x8892, StreamDraw = 0x88E0;
	public const uint VertexShader = 0x8B31, FragmentShader = 0x8B30, CompileStatus = 0x8B81, LinkStatus = 0x8B82, InfoLogLength = 0x8B84;
	public const uint Texture2D = 0x0DE1, Texture0 = 0x84C0;
	public const uint TextureMinFilter = 0x2801, TextureMagFilter = 0x2800, TextureWrapS = 0x2802, TextureWrapT = 0x2803, TextureMaxLevel = 0x813D;
	public const int Linear = 0x2601, LinearMipmapLinear = 0x2703, ClampToEdge = 0x812F, Repeat = 0x2901;
	public const int Rgba8 = 0x8058, Srgb8Alpha8 = 0x8C43;
	public const uint Rgba = 0x1908, Bgra = 0x80E1;
	public const uint UnpackAlignment = 0x0CF5;
	public const uint Framebuffer = 0x8D40, ReadFramebuffer = 0x8CA8, DrawFramebuffer = 0x8CA9, ColorAttachment0 = 0x8CE0, FramebufferComplete = 0x8CD5;
	public const uint Nearest = 0x2600;
	public const uint Version = 0x1F02, Renderer = 0x1F01;

	public static delegate* unmanaged<uint, IntPtr> GetString;
	public static delegate* unmanaged<uint> GetError;
	public static delegate* unmanaged<int, int, int, int, void> Viewport;
	public static delegate* unmanaged<float, float, float, float, void> ClearColor;
	public static delegate* unmanaged<uint, void> Clear;
	public static delegate* unmanaged<uint, void> Enable;
	public static delegate* unmanaged<uint, void> Disable;
	public static delegate* unmanaged<uint, uint, uint, uint, void> BlendFuncSeparate;

	public static delegate* unmanaged<uint, uint> CreateShader;
	public static delegate* unmanaged<uint, int, byte**, int*, void> ShaderSource;
	public static delegate* unmanaged<uint, void> CompileShader;
	public static delegate* unmanaged<uint, uint, int*, void> GetShaderiv;
	public static delegate* unmanaged<uint, int, int*, byte*, void> GetShaderInfoLog;
	public static delegate* unmanaged<uint, void> DeleteShader;
	public static delegate* unmanaged<uint> CreateProgram;
	public static delegate* unmanaged<uint, uint, void> AttachShader;
	public static delegate* unmanaged<uint, void> LinkProgram;
	public static delegate* unmanaged<uint, uint, int*, void> GetProgramiv;
	public static delegate* unmanaged<uint, int, int*, byte*, void> GetProgramInfoLog;
	public static delegate* unmanaged<uint, void> UseProgram;
	public static delegate* unmanaged<uint, byte*, int> GetUniformLocation;
	public static delegate* unmanaged<int, float, float, float, float, void> Uniform4f;
	public static delegate* unmanaged<int, int, void> Uniform1i;

	public static delegate* unmanaged<int, uint*, void> GenVertexArrays;
	public static delegate* unmanaged<uint, void> BindVertexArray;
	public static delegate* unmanaged<int, uint*, void> GenBuffers;
	public static delegate* unmanaged<uint, uint, void> BindBuffer;
	public static delegate* unmanaged<uint, nint, void*, uint, void> BufferData;
	public static delegate* unmanaged<uint, nint, nint, void*, void> BufferSubData;
	public static delegate* unmanaged<uint, int, uint, byte, int, nint, void> VertexAttribPointer;
	public static delegate* unmanaged<uint, int, uint, int, nint, void> VertexAttribIPointer;
	public static delegate* unmanaged<uint, void> EnableVertexAttribArray;
	public static delegate* unmanaged<uint, int, int, void> DrawArrays;

	public static delegate* unmanaged<int, uint*, void> GenTextures;
	public static delegate* unmanaged<int, uint*, void> DeleteTextures;
	public static delegate* unmanaged<uint, uint, void> BindTexture;
	public static delegate* unmanaged<uint, void> ActiveTexture;
	public static delegate* unmanaged<uint, int, int, int, int, int, uint, uint, void*, void> TexImage2D;
	public static delegate* unmanaged<uint, int, int, int, int, int, uint, uint, void*, void> TexSubImage2D;
	public static delegate* unmanaged<uint, uint, int, void> TexParameteri;
	public static delegate* unmanaged<uint, void> GenerateMipmap;
	public static delegate* unmanaged<uint, int, void> PixelStorei;

	public static delegate* unmanaged<int, uint*, void> GenFramebuffers;
	public static delegate* unmanaged<int, uint*, void> DeleteFramebuffers;
	public static delegate* unmanaged<uint, uint, void> BindFramebuffer;
	public static delegate* unmanaged<uint, uint, uint, uint, int, void> FramebufferTexture2D;
	public static delegate* unmanaged<uint, uint> CheckFramebufferStatus;
	public static delegate* unmanaged<int, int, int, int, int, int, int, int, uint, uint, void> BlitFramebuffer;
	public static delegate* unmanaged<int, int, int, int, uint, uint, void*, void> ReadPixels;

	public static void Load()
	{
		static IntPtr P( string name )
		{
			var p = Sdl.SDL_GL_GetProcAddress( name );
			if ( p == IntPtr.Zero ) throw new InvalidOperationException( $"OpenGL has no {name}" );
			return p;
		}

		GetString = (delegate* unmanaged<uint, IntPtr>)P( "glGetString" );
		GetError = (delegate* unmanaged<uint>)P( "glGetError" );
		Viewport = (delegate* unmanaged<int, int, int, int, void>)P( "glViewport" );
		ClearColor = (delegate* unmanaged<float, float, float, float, void>)P( "glClearColor" );
		Clear = (delegate* unmanaged<uint, void>)P( "glClear" );
		Enable = (delegate* unmanaged<uint, void>)P( "glEnable" );
		Disable = (delegate* unmanaged<uint, void>)P( "glDisable" );
		BlendFuncSeparate = (delegate* unmanaged<uint, uint, uint, uint, void>)P( "glBlendFuncSeparate" );

		CreateShader = (delegate* unmanaged<uint, uint>)P( "glCreateShader" );
		ShaderSource = (delegate* unmanaged<uint, int, byte**, int*, void>)P( "glShaderSource" );
		CompileShader = (delegate* unmanaged<uint, void>)P( "glCompileShader" );
		GetShaderiv = (delegate* unmanaged<uint, uint, int*, void>)P( "glGetShaderiv" );
		GetShaderInfoLog = (delegate* unmanaged<uint, int, int*, byte*, void>)P( "glGetShaderInfoLog" );
		DeleteShader = (delegate* unmanaged<uint, void>)P( "glDeleteShader" );
		CreateProgram = (delegate* unmanaged<uint>)P( "glCreateProgram" );
		AttachShader = (delegate* unmanaged<uint, uint, void>)P( "glAttachShader" );
		LinkProgram = (delegate* unmanaged<uint, void>)P( "glLinkProgram" );
		GetProgramiv = (delegate* unmanaged<uint, uint, int*, void>)P( "glGetProgramiv" );
		GetProgramInfoLog = (delegate* unmanaged<uint, int, int*, byte*, void>)P( "glGetProgramInfoLog" );
		UseProgram = (delegate* unmanaged<uint, void>)P( "glUseProgram" );
		GetUniformLocation = (delegate* unmanaged<uint, byte*, int>)P( "glGetUniformLocation" );
		Uniform4f = (delegate* unmanaged<int, float, float, float, float, void>)P( "glUniform4f" );
		Uniform1i = (delegate* unmanaged<int, int, void>)P( "glUniform1i" );

		GenVertexArrays = (delegate* unmanaged<int, uint*, void>)P( "glGenVertexArrays" );
		BindVertexArray = (delegate* unmanaged<uint, void>)P( "glBindVertexArray" );
		GenBuffers = (delegate* unmanaged<int, uint*, void>)P( "glGenBuffers" );
		BindBuffer = (delegate* unmanaged<uint, uint, void>)P( "glBindBuffer" );
		BufferData = (delegate* unmanaged<uint, nint, void*, uint, void>)P( "glBufferData" );
		BufferSubData = (delegate* unmanaged<uint, nint, nint, void*, void>)P( "glBufferSubData" );
		VertexAttribPointer = (delegate* unmanaged<uint, int, uint, byte, int, nint, void>)P( "glVertexAttribPointer" );
		VertexAttribIPointer = (delegate* unmanaged<uint, int, uint, int, nint, void>)P( "glVertexAttribIPointer" );
		EnableVertexAttribArray = (delegate* unmanaged<uint, void>)P( "glEnableVertexAttribArray" );
		DrawArrays = (delegate* unmanaged<uint, int, int, void>)P( "glDrawArrays" );

		GenTextures = (delegate* unmanaged<int, uint*, void>)P( "glGenTextures" );
		DeleteTextures = (delegate* unmanaged<int, uint*, void>)P( "glDeleteTextures" );
		BindTexture = (delegate* unmanaged<uint, uint, void>)P( "glBindTexture" );
		ActiveTexture = (delegate* unmanaged<uint, void>)P( "glActiveTexture" );
		TexImage2D = (delegate* unmanaged<uint, int, int, int, int, int, uint, uint, void*, void>)P( "glTexImage2D" );
		TexSubImage2D = (delegate* unmanaged<uint, int, int, int, int, int, uint, uint, void*, void>)P( "glTexSubImage2D" );
		TexParameteri = (delegate* unmanaged<uint, uint, int, void>)P( "glTexParameteri" );
		GenerateMipmap = (delegate* unmanaged<uint, void>)P( "glGenerateMipmap" );
		PixelStorei = (delegate* unmanaged<uint, int, void>)P( "glPixelStorei" );

		GenFramebuffers = (delegate* unmanaged<int, uint*, void>)P( "glGenFramebuffers" );
		DeleteFramebuffers = (delegate* unmanaged<int, uint*, void>)P( "glDeleteFramebuffers" );
		BindFramebuffer = (delegate* unmanaged<uint, uint, void>)P( "glBindFramebuffer" );
		FramebufferTexture2D = (delegate* unmanaged<uint, uint, uint, uint, int, void>)P( "glFramebufferTexture2D" );
		CheckFramebufferStatus = (delegate* unmanaged<uint, uint>)P( "glCheckFramebufferStatus" );
		BlitFramebuffer = (delegate* unmanaged<int, int, int, int, int, int, int, int, uint, uint, void>)P( "glBlitFramebuffer" );
		ReadPixels = (delegate* unmanaged<int, int, int, int, uint, uint, void*, void>)P( "glReadPixels" );
	}

	public static string Text( uint name ) => Marshal.PtrToStringUTF8( GetString( name ) ) ?? "";
}
