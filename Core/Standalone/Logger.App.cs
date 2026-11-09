namespace Rinten.Diagnostics;

//An app's log is its standard error, warnings and errors only unless RINTEN_LOG=trace|info says otherwise
public partial class Logger
{
	private static readonly LogLevel least = Environment.GetEnvironmentVariable( "RINTEN_LOG" ) switch
	{
		"trace" => LogLevel.Trace,
		"info" => LogLevel.Info,
		"error" => LogLevel.Error,
		_ => LogLevel.Warn,
	};

	partial void Write( LogLevel level, Exception exception, FormattableString message, string name )
	{
		if ( level < least ) return;

		var line = $"[{level}] {name}: {message}";
		if ( exception is not null ) line += $"\n{exception}";

		Console.Error.WriteLine( line );
	}
}
