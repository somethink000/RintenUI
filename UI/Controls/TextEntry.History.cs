namespace Rinten.UI;

public partial class TextEntry
{
	List<string> _history { get; set; } = new();

	/// <summary>Maximum amount of items <see cref="AddToHistory"/> will keep. Oldest items will be discarded as new ones are added.</summary>
	public int HistoryMaxItems { get; set; } = 30;

	string _historyCookie;
	/// <summary>If set, the history is kept between runs under this key - the game's cookies in the engine, UiHost.LoadKept in an app.</summary>
	public string HistoryCookie
	{
		get => _historyCookie;
		set
		{
			if ( _historyCookie == value ) return;

			_historyCookie = value;

			if ( string.IsNullOrEmpty( _historyCookie ) )
				return;

			_history = UiPlatform.Kept( value, _history );
		}
	}

	/// <summary>Add given string to the history of this text entry. The history can be accessed by the player by pressing up and down arrows with an empty text entry.</summary>
	public void AddToHistory( string str )
	{
		_history.RemoveAll( x => x == str );
		_history.Add( str );

		if ( HistoryMaxItems > 0 )
		{
			while ( _history.Count > HistoryMaxItems )
			{
				_history.RemoveAt( 0 );
			}
		}

		if ( !string.IsNullOrEmpty( HistoryCookie ) )
		{
			UiPlatform.Keep( HistoryCookie, _history );
		}
	}

	/// <summary>Clear the input history that was previously added via <see cref="AddToHistory"/>.</summary>
	public void ClearHistory()
	{
		_history.Clear();

		if ( !string.IsNullOrEmpty( HistoryCookie ) )
		{
			UiPlatform.Keep( HistoryCookie, _history );
		}
	}
}
