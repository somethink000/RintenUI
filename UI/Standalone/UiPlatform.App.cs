using System.Reflection;

namespace Rinten.UI;

//An app's answers: plain reflection over its loaded assemblies, and the pointer its window reports
internal static partial class UiPlatform
{
	private static Dictionary<string, Type> panelTypes;

	//Element names to panel types, found once across what is loaded; [Library] and [Alias] names win over class names
	private static Dictionary<string, Type> PanelTypes()
	{
		if ( panelTypes is not null ) return panelTypes;

		var found = new Dictionary<string, Type>( StringComparer.OrdinalIgnoreCase );

		foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
		{
			if ( assembly.IsDynamic ) continue;

			Type[] types;
			try { types = assembly.GetTypes(); }
			catch ( ReflectionTypeLoadException e ) { types = e.Types.Where( t => t is not null ).ToArray(); }

			foreach ( var type in types )
			{
				if ( type.IsAbstract || !typeof( Panel ).IsAssignableFrom( type ) || type.GetConstructor( Type.EmptyTypes ) is null ) continue;

				found.TryAdd( type.Name, type );
			}

			foreach ( var type in types )
			{
				if ( type.IsAbstract || !typeof( Panel ).IsAssignableFrom( type ) || type.GetConstructor( Type.EmptyTypes ) is null ) continue;

				if ( type.GetCustomAttribute<LibraryAttribute>( false )?.Name is { } name ) found[name] = type;

				foreach ( var alias in type.GetCustomAttribute<AliasAttribute>( false )?.Value ?? [] )
					found[alias] = type;
			}
		}

		return panelTypes = found;
	}

	internal static partial Panel CreatePanel( string elementName )
		=> PanelTypes().TryGetValue( elementName, out var type ) ? (Panel)Activator.CreateInstance( type ) : null;

	internal static partial void SetProperty( object target, string name, string value )
	{
		var property = target.GetType().GetProperty( name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase );
		if ( property is null || !property.CanWrite ) return;

		if ( Translation.TryConvert( value, property.PropertyType, out var converted ) )
			property.SetValue( target, converted );
	}

	internal static partial bool Describes( Type type ) => true;

	internal static partial string PrepareSheet( string text, string file ) => text;

	internal static partial Vector2 PointerPosition { get => field; set => field = value; }
	internal static partial Vector2 PointerDelta { get => Pointer.Delta; }
	internal static partial Vector2 PointerVelocity { get => Pointer.Velocity; }
	internal static partial bool PointerVisible { get => Pointer.Visible; }
	internal static partial void SetCursor( string name ) => Pointer.Cursor = name;

	internal static partial void VirtualCursor( ref bool leftDown, ref Vector2 wheel ) { }

	internal static partial void PlaySound( Panel panel, string sound ) => UiHost.PlaySound( sound );

	internal static partial string Phrase( string token ) => token;

	internal static partial void ClosePopups( Panel pressed ) { }

	internal static partial bool Fetch( string path, Action arrived ) => false;

	internal static partial void For<TState>( int count, TState state, Action<TState, int> body )
		=> System.Threading.Tasks.Parallel.For( 0, count, i => body( state, i ) );

	internal static partial bool InOrder { get => false; }

	internal static partial ITextureHost Textures { get => UiHost.Textures; }
	internal static partial IUiDevice Device { get => UiHost.Device; }

	internal static partial long Frame() => UiHost.Frame;
	internal static partial double Now() => RealTime.NowDouble;
	internal static partial float TimeScale() => 1.0f;

	internal static partial Rect Screen() => UiHost.Screen;

	internal static partial void PumpAsync()
	{
		MainThread.RunQueues();
		Rinten.Tasks.SyncContext.MainThread?.ProcessQueue();
	}

	internal static partial void Spin() { }

	internal static partial void InputTicked( UISystem ui ) { }
	internal static partial void TickWorldInput( UISystem ui ) { }
	internal static partial bool? PointerForced() => null;
	internal static partial bool HidesPointer( RootPanel root ) => false;

	internal static partial UiScope Scope( string name ) => default;

	internal static partial bool IsHeadless { get => UiHost.IsHeadless; }
	internal static partial bool OnWeb { get => OperatingSystem.IsBrowser(); }

	internal static partial void SetClipboard( string text ) => UiHost.SetClipboard( text );
}

/// <summary>The pointer as the window an app runs in reports it.</summary>
public static class Pointer
{
	/// <summary>Where it is, in the window's pixels.</summary>
	public static Vector2 Position { get => UiPlatform.PointerPosition; set => UiPlatform.PointerPosition = value; }

	/// <summary>How far it moved since the last frame.</summary>
	public static Vector2 Delta { get; set; }

	/// <summary>How fast it is moving, in pixels a second.</summary>
	public static Vector2 Velocity { get; set; }

	/// <summary>Whether it is shown.</summary>
	public static bool Visible { get; set; } = true;

	/// <summary>The cursor the panel under it asks for; null for the default.</summary>
	public static string Cursor { get; set; }
}

//An app's profiler sections are nothing
internal ref partial struct UiScope
{
	public void Dispose() { }
}
