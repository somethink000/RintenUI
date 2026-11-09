using Rinten;

namespace Microsoft.AspNetCore.Components;

public partial class RouteAttribute : System.Attribute
{
	/// <summary>The full url of this route (ie "/home/section/page")</summary>
	public string Url { get; }

	/// <summary>The url split into parts (ie "home" "section" "page" )</summary>
	public string[] Parts { get; }

	public RouteAttribute( string url )
	{
		Url = url;
		Parts = Url.Split( '/', StringSplitOptions.RemoveEmptyEntries );
	}

	bool TestPart( string part, string ours )
	{
		//this is a variable
		if ( ours != null && ours.StartsWith( '{' ) && ours.EndsWith( '}' ) )
			return true;

		return part == ours;
	}

	/// <summary>True if this matches the passed in url. Queries are trimmed and ignored <c>( ?query=fff )</c> Variables are tested (but not type matched or anything)</summary>
	public bool IsUrl( string url )
	{
		if ( string.IsNullOrEmpty( url ) ) return false;

		if ( url.Contains( '?' ) )
		{
			url = url[..url.IndexOf( '?' )];
		}

		var a = url.Split( '/', StringSplitOptions.RemoveEmptyEntries );

		for ( int i = 0; i < Parts.Length || i < a.Length; i++ )
		{
			var left = i < a.Length ? a[i] : null;
			var right = i < Parts.Length ? Parts[i] : null;

			if ( right == "*" )
				return true;

			if ( !TestPart( left, right ) )
				return false;
		}

		return true;
	}

	/// <summary>Given a Url, check for {properties} and convert them to key values</summary>
	public IEnumerable<(string key, string value)> ExtractProperties( string url )
	{
		var a = url.Split( '/', StringSplitOptions.RemoveEmptyEntries );

		for ( int i = 0; i < Parts.Length; i++ )
		{
			if ( !Parts[i].StartsWith( '{' ) ) continue;
			if ( !Parts[i].EndsWith( '}' ) ) continue;

			var key = Parts[i][1..^1].Trim( '?' );

			if ( i < a.Length )
			{
				yield return (key, a[i]);
			}
			else
			{
				yield return (key, null);
			}
		}
	}
}
