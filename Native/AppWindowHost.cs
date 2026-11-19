namespace Rinten.UI.Native;

//The window as the toolkit's IAppWindow, so a title bar in Rinten.UI can drive it - see UiHost.Window
internal sealed class AppWindowHost : IAppWindow
{
	public bool IsMaximized => AppWindow.IsMaximized;

	public bool Frameless
	{
		get => AppWindow.Frameless;
		set => AppWindow.Frameless = value;
	}

	public void Minimize() => AppWindow.Minimize();
	public void ToggleMaximized() => AppWindow.ToggleMaximized();
	public void Close() => AppWindow.Close();
	public void SetTitle( string title ) => AppWindow.SetTitle( title );
	public void SetDragAreas( params Rect[] areas ) => AppWindow.SetDragAreas( areas );
}
