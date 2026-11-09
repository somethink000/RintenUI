using System.Threading;

namespace Rinten.UI;

//What panels, stylesheets and tasks belong to, as one: the engine has one for its menu and one for the game, an app has one
internal partial class UiContext
{
	/// <summary>The interface system that ticks this context's root panels.</summary>
	public UISystem UISystem { get; set; }

	/// <summary>Where stylesheets, fonts and pictures are read from.</summary>
	public BaseFileSystem FileMount { get; set; }

	/// <summary>Tasks that end with this context.</summary>
	public TaskSource TaskSource { get; set; }

	/// <summary>Cancelled when this context ends.</summary>
	public CancellationTokenSource CancellationTokenSource { get; set; }

	/// <summary>Whether this is the engine's menu rather than a game.</summary>
	public virtual bool IsMenu => false;

	/// <summary>The context running now.</summary>
	public static UiContext Current => CurrentContext();

	/// <summary>Makes this the current context until the result is disposed.</summary>
	public virtual IDisposable Enter() => null;

	/// <summary>The context whose interface system holds this root.</summary>
	public static UiContext Of( RootPanel root ) => OwnerOf( root ) ?? Current;

	//The host's own answers: the engine's GlobalContext, or the one context of an app
	private static partial UiContext CurrentContext();
	private static partial UiContext OwnerOf( RootPanel root );
}
