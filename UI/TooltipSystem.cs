using Rinten.Internal;

namespace Rinten.UI;

internal static class TooltipSystem
{
	static IPanel lastHovered;
	static IPanel lastTooltip;

	internal static void Clear()
	{
		lastTooltip?.Delete( true );
		lastTooltip = null;
		lastHovered = null;
	}

	internal static void SetHovered( IPanel current )
	{
		if ( lastHovered == current )
			return;

		if ( current == null )
		{
			lastHovered = null;
			lastTooltip?.Delete( false );
			lastTooltip = null;
			return;
		}

		if ( !current.HasTooltip )
		{
			SetHovered( current.Parent );
			return;
		}

		lastHovered = current;

		if ( current != null )
		{
			lastTooltip?.Delete( false );
			lastTooltip = current.CreateTooltip();
			Frame();
		}
	}

	internal static void Frame()
	{
		if ( !lastTooltip.IsValid() )
			return;

		if ( !UiPlatform.PointerVisible )
		{
			lastTooltip?.Delete( false );
			lastTooltip = null;
			lastHovered = null;
			return;
		}

		if ( lastHovered.IsValid() )
		{
			lastHovered.UpdateTooltip( lastTooltip );
		}

		//Given the mouse position, try to position the tooltip so it's not hanging off
		//the screen. Not actually doing any kind of restrict to screen or anything.

		var pos = UiPlatform.PointerPosition;

		TextFlag align = 0;

		if ( pos.x < UiPlatform.Screen().Size.x * 0.70f )
		{
			align |= TextFlag.Right;
		}
		else
		{
			align |= TextFlag.Left;
		}

		if ( pos.y > UiPlatform.Screen().Size.y * 0.1f )
		{
			align |= TextFlag.Top;
		}
		else
		{
			align |= TextFlag.Bottom;
		}

		lastTooltip.SetAbsolutePosition( align, pos, 20 );

	}
}
