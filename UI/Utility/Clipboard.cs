
namespace Rinten.UI;

public static class Clipboard
{
	/// <summary>Sets the clipboard text</summary>
	public static void SetText( string text )
	{
		UiPlatform.SetClipboard( text );
	}

	/// <summary>The clipboard's text, or empty</summary>
	public static string GetText() => UiPlatform.GetClipboard() ?? "";
}
