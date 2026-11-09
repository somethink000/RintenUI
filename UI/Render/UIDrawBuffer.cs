using Rinten.Rendering;

namespace Rinten.UI;

//Collects Panel.Draw calls in editor windows, whose renderer builds vertices instead of descriptors
internal interface IUiDrawCollector
{
	void Box( in BoxDrawDescriptor desc );
	void Shadow( in ShadowDrawDescriptor desc );
	void Outline( in OutlineDrawDescriptor desc );

	//A vector path filled, or stroked where a stroke is given, in the space of Panel.Box.
	void Path( VectorPath path, Color color, in VectorStrokeStyle? stroke, FillRule rule );
}

//What a panel's OnDraw draws into: the walk sets a collector before calling it, and without one the calls go nowhere
internal class UIDrawBuffer
{
	[ThreadStatic] static UIDrawBuffer _current;

	internal static UIDrawBuffer Current => _current ??= new();

	/// <summary>Takes the draw calls while it is set.</summary>
	public IUiDrawCollector Collector;

	/// <summary>The scale factor from logical to physical pixels for the current panel. Set before OnDraw().</summary>
	public float ScaleToScreen = 1f;

	/// <summary>Accumulated CSS opacity for the current panel. Set before OnDraw().</summary>
	public float Opacity = 1f;

	/// <summary>Active blend mode override for the current panel. Set before OnDraw().</summary>
	public BlendMode OverrideBlendMode = BlendMode.Normal;

	public void AddBox( in BoxDrawDescriptor desc )
	{
		Collector?.Box( desc );
	}

	public void AddShadow( in ShadowDrawDescriptor desc )
	{
		Collector?.Shadow( desc );
	}

	public void AddOutline( in OutlineDrawDescriptor desc )
	{
		Collector?.Outline( desc );
	}

	/// <summary>A vector path, filled or stroked.</summary>
	public void AddPath( VectorPath path, Color color, in VectorStrokeStyle? stroke, FillRule rule )
	{
		if ( path is null || path.IsEmpty ) return;

		Collector?.Path( path, color, stroke, rule );
	}

}
