using System.Threading;

namespace Rinten.UI;

//An app has one context for its whole life
internal partial class UiContext
{
	private static UiContext app;

	private static partial UiContext CurrentContext()
	{
		if ( app is not null ) return app;

		//Set before the task source is made, which asks for the current context's token
		app = new UiContext
		{
			UISystem = new UISystem(),
			FileMount = new LocalFileSystem( AppContext.BaseDirectory ),
			CancellationTokenSource = new CancellationTokenSource(),
		};

		app.TaskSource = TaskSource.Create( app.CancellationTokenSource.Token );
		return app;
	}

	private static partial UiContext OwnerOf( RootPanel root ) => app;
}
