using Microsoft.AspNetCore.Components;
using Rinten.UI.Construct;

namespace Rinten.UI;

/// <summary>A number picked by dragging a thumb along a track.</summary>
[Library( "slider" )]
public class Slider : Panel
{
	/// <summary>Called with the new value whenever the user moves the thumb.</summary>
	[Parameter] public System.Action<float> OnValueChanged { get; set; }

	/// <summary>Called once with the value the user let go at - for what is too costly to follow every move.</summary>
	[Parameter] public System.Action<float> OnValueCommitted { get; set; }

	/// <summary>Whether the thumb is held right now; a value polled from elsewhere should wait until it is let go.</summary>
	public bool Dragging => HasActive;

	/// <summary>The value at the left end of the track.</summary>
	[Parameter] public float Min { get => min; set { min = value; UpdateVisuals(); } }

	/// <summary>The value at the right end of the track.</summary>
	[Parameter] public float Max { get => max; set { max = value; UpdateVisuals(); } }

	/// <summary>What the value snaps to; 0 for no snapping.</summary>
	[Parameter] public float Step { get; set; } = 0.001f;

	/// <summary>The current value.</summary>
	[Parameter]
	public float Value
	{
		get => value;
		set
		{
			if ( this.value == value ) return;

			this.value = value;
			UpdateVisuals();
		}
	}

	float min, max = 100.0f, value;

	readonly Panel track;
	readonly Panel fill;
	readonly Panel thumb;

	//The look every slider starts from; a game's or an app's own sheet goes over it
	const string Styles = """
		.slider
		{
			flex-direction: column;
			justify-content: center;
			min-width: 50px;
			min-height: 24px;
			flex-grow: 1;
			flex-shrink: 0;
			cursor: pointer;
			pointer-events: all;

			> .track
			{
				position: relative;
				height: 6px;
				margin: 9px 8px;
				align-items: center;
				border-radius: 3px;
				background-color: #3a3e47;

				> .track-active
				{
					position: absolute;
					left: 0px;
					height: 100%;
					border-radius: 3px;
					background-color: #3273EB;
				}

				> .thumb
				{
					position: relative;
					width: 14px;
					height: 14px;
					border-radius: 100px;
					background-color: #fff;
					transform: translateX( -50% );
					box-shadow: 0 1px 4px #000a;
				}
			}

			&:hover > .track > .thumb
			{
				box-shadow: 0 1px 4px #000a, 0 0 0 4px #3273EB40;
			}
		}
		""";

	static Rinten.UI.StyleSheet sheet;

	public Slider()
	{
		StyleSheet.Add( sheet ??= Rinten.UI.StyleSheet.FromString( Styles, "slider" ) );
		AddClass( "slider" );

		track = Add.Panel( "track" );
		fill = track.Add.Panel( "track-active" );
		thumb = track.Add.Panel( "thumb" );

		UpdateVisuals();
	}

	void UpdateVisuals()
	{
		if ( thumb is null ) return;

		var position = MathX.LerpInverse( value, min, max, true ) * 100.0f;

		fill.Style.Width = Length.Percent( position );
		thumb.Style.Left = Length.Percent( position );
	}

	//Clamped to the track, then snapped
	float ValueAtMouse()
	{
		var width = track.Box.Rect.Width;
		var along = width > 0.0f ? track.MousePosition.x / width : 0.0f;
		var scaled = MathX.LerpTo( min, max, along, true );

		return Step > 0.0f ? MathF.Round( scaled / Step ) * Step : scaled;
	}

	void SetFromMouse( MousePanelEvent e )
	{
		Value = ValueAtMouse();
		OnValueChanged?.Invoke( Value );
		e.StopPropagation();
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );
		SetFromMouse( e );
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );
		OnValueCommitted?.Invoke( Value );
	}

	protected override void OnMouseMove( MousePanelEvent e )
	{
		base.OnMouseMove( e );

		if ( !HasActive || e.MouseButton == MouseButtons.Middle ) return;

		SetFromMouse( e );
	}
}
