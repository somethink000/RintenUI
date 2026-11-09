using System.Web;

namespace Rinten.Diagnostics;

[StackTraceHidden]
public partial class Logger
{
	/// <summary>Name of this logger.</summary>
	public string Name { get; protected set; }

	public Logger( string name )
	{
		Name = name;
		Created();
	}

	internal void DoInfo( FormattableString message ) => Emit( LogLevel.Info, null, message );
	internal void DoTrace( FormattableString message ) => Emit( LogLevel.Trace, null, message );
	internal void DoWarning( FormattableString message ) => Emit( LogLevel.Warn, null, message );
	internal void DoError( FormattableString message ) => Emit( LogLevel.Error, null, message );

	public void Info( FormattableString message ) => DoInfo( message );
	public void Trace( FormattableString message ) => DoTrace( message );
	public void Warning( FormattableString message ) => DoWarning( message );
	public void Error( FormattableString message ) => DoError( message );

	public void Error( Exception exception, FormattableString message ) => Emit( LogLevel.Error, exception, message );

	/// <summary>Log an exception as an error, with given message override.</summary>
	public void Error( Exception exception, object message ) => Error( exception, $"{message}" );

	/// <summary>Log an exception as an error.</summary>
	public void Error( Exception exception ) => Emit( LogLevel.Error, exception, $"{exception.Message}" );

	public void Warning( Exception exception, FormattableString message ) => Emit( LogLevel.Warn, exception, message );

	/// <summary>Log an exception as a warning, with given message override.</summary>
	public void Warning( Exception exception, object message ) => Warning( exception, $"{message}" );

	/// <summary>Log some information. This is the default log severity level.</summary>
	public void Info( object message )
	{
		if ( message is Exception ex )
		{
			Emit( LogLevel.Info, ex, $"{ex.Message}" );
			return;
		}

		Info( $"{message}" );
	}

	/// <summary>Log some information. This is least severe log level.</summary>
	public void Trace( object message )
	{
		if ( message is Exception ex )
		{
			Emit( LogLevel.Trace, ex, $"{ex.Message}" );
			return;
		}

		Trace( $"{message}" );
	}

	/// <summary>Log a warning. This is the second most severe log level.</summary>
	public void Warning( object message )
	{
		if ( message is Exception ex )
		{
			Warning( ex, ex.Message );
			return;
		}

		Warning( $"{message}" );
	}

	/// <summary>Log an error. This is the most severe log level.</summary>
	public void Error( object message )
	{
		if ( message is Exception ex )
		{
			Error( ex, ex.Message );
			return;
		}

		Error( $"{message}" );
	}

	//Where a message goes is the host's: the engine's log and console, or an app's standard error
	internal void Emit( LogLevel level, Exception exception, FormattableString message, string name = null ) => Write( level, exception, message, name ?? Name );

	partial void Created();
	partial void Write( LogLevel level, Exception exception, FormattableString message, string name );

	//Wrap / escape an object for html log messages. Inspectable objects will be wrapped in a link, and added to outArgs. The
	//link will index into outArgs. FormattableStrings will recurse into WrapObject, so their arguments can also be inspected.
	internal static object WrapObject( object o, List<object> outArgs )
	{
		if ( o == null ) return "null";

		if ( o is FormattableString formattable )
		{
			var wrappedArgs = formattable.GetArguments()
				.Select( x => WrapObject( x, outArgs ) )
				.ToArray();

			return string.Format( formattable.Format, wrappedArgs );
		}

		if ( o is string ) return HttpUtility.HtmlEncode( o );

		var t = o.GetType();
		if ( t.IsPrimitive ) return o;

		var index = outArgs.Count;
		outArgs.Add( o );

		return $"<a href=\"arg:{index}\" style=\"\">{HttpUtility.HtmlEncode( o.ToString() )}</a>";
	}

}
