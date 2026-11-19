#nullable enable

namespace Rinten.UI.NodeGraph;

/// <summary>A menu of the graph: options, separators and named groups, shown as one list where it is opened.</summary>
public class GraphMenu
{
	/// <summary>One step of where an option sits: every step but the last names its group.</summary>
	public record struct PathElement( string Name, string? Icon = null, string? Description = null, int? Order = null, bool IsHeading = false );

	/// <summary>One line of a menu; a host that shows menus its own way reads these.</summary>
	public abstract record Entry;
	public sealed record Option( string Text, string? Icon, Action? Action, bool Enabled = true ) : Entry;
	public sealed record Separator : Entry;
	public sealed record Sub( GraphMenu Menu ) : Entry;

	/// <summary>A text field in the menu: a name typed in, or a filter over the rest of it.</summary>
	public sealed record Field( string Label, string? Value, string? Placeholder, bool AutoFocus, Action<string>? OnSubmit, Action<string>? OnChange ) : Entry;

	internal readonly List<Entry> Entries = new();

	/// <summary>What the menu holds, in order.</summary>
	public IReadOnlyList<Entry> Items => Entries;

	/// <summary>The group heading this menu is shown under inside another one.</summary>
	public string? Title { get; }

	/// <summary>A Material Icons glyph beside the title.</summary>
	public string? Icon { get; set; }

	/// <summary>Raised when the menu's contents were changed while it is shown, as a filter does; whoever shows it shows them again.</summary>
	public event Action? Changed;

	//The menu this one was added to, which shows it - see Refresh
	private GraphMenu? parent;

	/// <summary>Says the contents changed - see Changed. Goes up to the menu that is actually shown.</summary>
	public void Refresh()
	{
		Changed?.Invoke();
		parent?.Refresh();
	}

	public GraphMenu( string? title = null ) => Title = title;

	public int OptionCount => Entries.Count( x => x is Option );
	public int MenuCount => Entries.Count( x => x is Sub );

	/// <summary>Raised just before the menu is shown.</summary>
	public event Action? AboutToShow;

	/// <summary>Raised when the menu goes away, picked from or not.</summary>
	public event Action? AboutToHide;

	public void AddOption( string text, string? icon = null, Action? action = null, bool enabled = true ) => Entries.Add( new Option( text, icon, action, enabled ) );

	public void AddSeparator() => Entries.Add( new Separator() );

	public void AddLineEdit( string label, string? value = null, string? placeholder = null, bool autoFocus = false,
		Action<string>? onSubmit = null, Action<string>? onChange = null )
		=> Entries.Add( new Field( label, value, placeholder, autoFocus, onSubmit, onChange ) );

	public GraphMenu AddMenu( string title, string? icon = null ) => AddMenu( new GraphMenu( title ) { Icon = icon } );

	public GraphMenu AddMenu( GraphMenu menu )
	{
		menu.parent = this;
		Entries.Add( new Sub( menu ) );
		return menu;
	}

	public void Clear() => Entries.Clear();

	/// <summary>One option per item, grouped by every step of its path but the last; flat puts them all in one list.</summary>
	public void AddOptions<T>( IEnumerable<T> items, Func<T, PathElement[]> getPath, Action<T> action, bool flat = false )
	{
		var groups = new Dictionary<string, GraphMenu>();

		foreach ( var (item, path) in items.Select( x => (x, getPath( x )) ).OrderBy( x => x.Item2[^1].Order ?? 0 ).ThenBy( x => x.Item2[^1].Name ) )
		{
			var leaf = path[^1];
			var target = this;

			if ( !flat && path.Length > 1 )
			{
				var key = string.Join( " / ", path[..^1].Select( x => x.Name ) );
				if ( !groups.TryGetValue( key, out target ) ) groups[key] = target = AddMenu( key );
			}

			target.AddOption( leaf.Name, leaf.Icon, () => action( item ) );
		}
	}

	/// <summary>Shows the menu over a panel, at a point in that panel's own pixels.</summary>
	public void OpenAt( Panel host, Vector2 position )
	{
		Showing();
		_ = new GraphMenuPanel( this, host, position );
	}

	/// <summary>Says the menu went away - for a host that shows menus its own way.</summary>
	public void Hidden() => AboutToHide?.Invoke();

	/// <summary>Says the menu is about to show - for a host that shows menus its own way.</summary>
	public void Showing() => AboutToShow?.Invoke();
}

//A catcher over the whole host that closes the menu on any press outside it, and the list itself
internal sealed class GraphMenuPanel : Panel
{
	private readonly GraphMenu _menu;
	private readonly Panel _list;
	private bool _closed;

	public GraphMenuPanel( GraphMenu menu, Panel host, Vector2 position )
	{
		_menu = menu;
		AddClass( "graph-menu-catcher" );
		host.AddChild( this );

		AddEventListener( "onmousedown", e =>
		{
			if ( e.Target == this ) Close();
		} );

		_list = AddChild<Panel>( "graph-menu" );
		var scale = ScaleFromScreen;
		_list.Style.Left = Length.Pixels( position.x * scale );
		_list.Style.Top = Length.Pixels( position.y * scale );

		Fill( _list, menu );
		menu.Changed += Refill;
		Focus();
	}

	//The rows again after a filter changed them; the fields stay, so what is being typed keeps its focus
	private void Refill()
	{
		foreach ( var child in _list.Children.ToArray() )
			if ( child is not TextEntry ) child.Delete( true );

		Fill( _list, _menu, fields: false );
	}

	private void Fill( Panel list, GraphMenu menu, bool fields = true )
	{
		foreach ( var entry in menu.Entries )
		{
			switch ( entry )
			{
				case GraphMenu.Field field:
					if ( !fields ) break;
					var entryField = list.AddChild<TextEntry>( "graph-menu-field" );
					entryField.Placeholder = field.Placeholder ?? field.Label;
					entryField.Text = field.Value ?? "";
					if ( field.OnChange is { } changed ) entryField.OnTextEdited = changed;
					if ( field.OnSubmit is { } submit ) entryField.AddEventListener( "onsubmit", () => { Close(); submit( entryField.Text ?? "" ); } );
					if ( field.AutoFocus ) entryField.Focus();
					break;

				case GraphMenu.Separator:
					list.AddChild<Panel>( "graph-menu-separator" );
					break;

				case GraphMenu.Sub sub:
					sub.Menu.Showing();
					if ( sub.Menu.Entries.Count == 0 ) break;
					if ( sub.Menu.Title is { } title ) list.AddChild<Label>( "graph-menu-heading" ).Text = title;
					Fill( list, sub.Menu, fields );
					break;

				case GraphMenu.Option option:
					var row = list.AddChild<Panel>( "graph-menu-row" );
					row.SetClass( "disabled", !option.Enabled );
					row.AddChild( new IconPanel( option.Icon ?? "", "icon" ) );
					row.AddChild<Label>( "text" ).Text = option.Text;
					row.AddEventListener( "onclick", () =>
					{
						if ( !option.Enabled ) return;
						Close();
						option.Action?.Invoke();
					} );
					break;
			}
		}
	}

	public override void OnButtonEvent( ButtonEvent e )
	{
		base.OnButtonEvent( e );
		if ( e.Pressed && e.Button == "escape" ) Close();
	}

	public void Close()
	{
		if ( _closed ) return;
		_closed = true;
		_menu.Changed -= Refill;
		Delete( true );
		_menu.Hidden();
	}
}

/// <summary>The few colours the graph draws from code rather than from its sheet.</summary>
public static class GraphColors
{
	public static Color Red { get; set; } = Color.Parse( "#e0565b" ) ?? Color.Red;
	public static Color Background { get; set; } = Color.Parse( "#1b1b1d" ) ?? Color.Black;
}
