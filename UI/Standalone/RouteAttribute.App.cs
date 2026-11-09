using System.Reflection;

namespace Microsoft.AspNetCore.Components;

public partial class RouteAttribute
{
	/// <summary>The loaded page type that answers this url and not the one it is under.</summary>
	public static (Type Type, RouteAttribute Attribute)? FindValidTarget( string url, string parentUrl )
	{
		var routes = AppDomain.CurrentDomain.GetAssemblies()
			.Where( a => !a.IsDynamic )
			.SelectMany( a => { try { return a.GetTypes(); } catch ( ReflectionTypeLoadException e ) { return e.Types.Where( t => t is not null ); } } )
			.SelectMany( t => t.GetCustomAttributes<RouteAttribute>( false ).Select( r => (Type: t, Attribute: r) ) );

		var val = routes
			.Where( x => x.Attribute.IsUrl( url ) && !x.Attribute.IsUrl( parentUrl ) )
			.OrderByDescending( x => x.Attribute.Url.Count( c => c == '*' ) )
			.FirstOrDefault();

		return val.Type is null ? null : val;
	}
}
