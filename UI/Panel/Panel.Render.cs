namespace Rinten.UI;

public partial class Panel
{
	//Set wherever what this panel draws may have changed; every set also moves RenderVersion and the tree's TreeGeneration.
	internal bool IsRenderDirty
	{
		get => field;
		set
		{
			field = value;
			if ( value )
			{
				RenderVersion++;
				MoveGeneration();
			}
		}
	} = true;

	//Counts the times this panel was marked dirty to draw, so a cache of what it drew can tell whether it is still the same.
	internal uint RenderVersion;

	//Bumped when anything drawn or hit-testable in this root's tree may have changed; read on roots only
	internal uint TreeGeneration;

	//Moves the TreeGeneration of the tree this panel is in.
	internal void MoveGeneration()
	{
		var root = this;
		while ( root.Parent is { } up ) root = up;
		root.TreeGeneration++;
	}

	//What the flat render walk emitted for this panel last, kept by CoreUiRenderer while nothing about it changed.
	internal CoreUiRenderer.PanelGeometry Geometry;

	public void MarkRenderDirty()
	{
		IsRenderDirty = true;
	}

	/// <summary>Override this to draw custom graphics for this panel using the <see cref="Draw"/> API. <example> <code> public override void OnDraw() { var r = Box.RectInner; Draw.Rect( r, Color.Blue.WithAlpha( 0.2f ), cornerRadius: 4 ); Draw.Text( "Score: 100", r, 16, Color.White, TextFlag.Center ); } </code> </example></summary>
	public virtual void OnDraw()
	{
	}

	//Whether OnDraw only depends on state reported via MarkRenderDirty, so its output can be reused
	protected internal virtual bool RetainsDrawing => false;

	//Whether a walk that draws this panel itself should skip its OnDraw rather than draw it twice.
	internal virtual bool DrawnByWalk => false;

	[Obsolete( "Use OnDraw" )]
	public virtual void DrawContent( ref RenderState state )
	{
	}

	[Obsolete( "Use OnDraw" )]
	public virtual void DrawBackground( ref RenderState state )
	{
	}
}
