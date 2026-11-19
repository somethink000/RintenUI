using System.Runtime.InteropServices;

namespace Rinten.UI.Native;

//What is queued and not yet heard tells a player where its sound is, which is the clock a video follows
/// <summary>Sound to the default speakers: 16-bit samples written from any thread are played in order.</summary>
public sealed unsafe partial class AudioOut : IDisposable
{
	private const string Lib = "SDL3";
	private const uint InitAudio = 0x10, DefaultPlayback = 0xFFFFFFFF;
	private const int S16 = 0x8010;

	[StructLayout( LayoutKind.Sequential )]
	private struct Spec { public int Format, Channels, Freq; }

	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_InitSubSystem( uint flags );
	[LibraryImport( Lib )] private static partial IntPtr SDL_OpenAudioDeviceStream( uint device, Spec* spec, IntPtr callback, IntPtr user );
	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_PutAudioStreamData( IntPtr stream, byte* data, int length );
	[LibraryImport( Lib )] private static partial int SDL_GetAudioStreamQueued( IntPtr stream );
	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_ResumeAudioStreamDevice( IntPtr stream );
	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_PauseAudioStreamDevice( IntPtr stream );
	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_ClearAudioStream( IntPtr stream );
	[LibraryImport( Lib )] [return: MarshalAs( UnmanagedType.U1 )] private static partial bool SDL_SetAudioStreamGain( IntPtr stream, float gain );
	[LibraryImport( Lib )] private static partial void SDL_DestroyAudioStream( IntPtr stream );

	private IntPtr stream;

	/// <summary>Samples per second per channel.</summary>
	public int Rate { get; }

	public int Channels { get; }

	/// <summary>Bytes of one second of sound.</summary>
	public int BytesPerSecond => Rate * Channels * 2;

	private AudioOut( IntPtr stream, int rate, int channels ) => (this.stream, Rate, Channels) = (stream, rate, channels);

	/// <summary>Opens the speakers for 16-bit sound, paused; null when there is no sound device.</summary>
	public static AudioOut Open( int rate = 48000, int channels = 2 )
	{
		if ( !SDL_InitSubSystem( InitAudio ) ) return null;
		var spec = new Spec { Format = S16, Channels = channels, Freq = rate };
		var s = SDL_OpenAudioDeviceStream( DefaultPlayback, &spec, IntPtr.Zero, IntPtr.Zero );
		return s == IntPtr.Zero ? null : new AudioOut( s, rate, channels );
	}

	/// <summary>Queues samples, interleaved by channel.</summary>
	public void Write( ReadOnlySpan<byte> data )
	{
		if ( stream == IntPtr.Zero || data.IsEmpty ) return;
		fixed ( byte* p = data ) SDL_PutAudioStreamData( stream, p, data.Length );
	}

	/// <summary>Bytes written and not yet played.</summary>
	public int Queued => stream == IntPtr.Zero ? 0 : SDL_GetAudioStreamQueued( stream );

	public void Play() { if ( stream != IntPtr.Zero ) SDL_ResumeAudioStreamDevice( stream ); }

	public void Pause() { if ( stream != IntPtr.Zero ) SDL_PauseAudioStreamDevice( stream ); }

	/// <summary>Drops what is queued, as a seek does.</summary>
	public void Clear() { if ( stream != IntPtr.Zero ) SDL_ClearAudioStream( stream ); }

	/// <summary>Loudness: 0 silent, 1 as written, up to 4 louder.</summary>
	public float Volume { set { if ( stream != IntPtr.Zero ) SDL_SetAudioStreamGain( stream, Math.Clamp( value, 0, 4 ) ); } }

	public void Dispose()
	{
		if ( stream == IntPtr.Zero ) return;
		SDL_DestroyAudioStream( stream );
		stream = IntPtr.Zero;
	}
}
