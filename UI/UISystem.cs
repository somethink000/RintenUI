using Rinten.Internal;
using Rinten.UI;

namespace Rinten;

//Holds onto a list of root panels to tick, input and draw
internal partial class UISystem
{
	internal PanelInput Input { get; set; } = new();

	internal List<RootPanel> RootPanels = new();
	internal List<Panel> DeletionList = new();
	internal InputEventQueue InputEventQueue = new();

	//focus
	internal Panel CurrentFocus { get; set; }
	internal Panel NextFocus { get; set; }
	internal bool FocusPendingChange { get; set; }

	internal void AddRoot( RootPanel rootPanel )
	{
		if ( RootPanels.Contains( rootPanel ) )
			throw new System.Exception( "Adding root panel twice" );

		RootPanels.Add( rootPanel );
	}

	internal void RemoveRoot( RootPanel rootPanel )
	{
		RootPanels.Remove( rootPanel );
	}

	internal void DeleteAllRoots()
	{
		var deleteList = new List<RootPanel>( RootPanels );
		foreach ( var rootPanel in deleteList )
		{
			rootPanel.Delete( true );

			//User can override Delete/OnDelete, so let's make sure we always remove from lists
			rootPanel.RemoveFromLists();
			RootPanels.Remove( rootPanel );
		}

		RunDeferredDeletion( true );
		DeletionList.Clear();
	}

	internal void Simulate( bool allowMouseInput )
	{
		Rect screen;

		using ( UiPlatform.Scope( "Update Screen Size" ) )
		{
			screen = UiPlatform.Screen();
		}

		using ( UiPlatform.Scope( "Tick Panels" ) )
		{
			TickPanels();
		}

		using ( UiPlatform.Scope( "Tick Input" ) )
		{
			TickInput( allowMouseInput );
		}

		using ( UiPlatform.Scope( "Pre Layout" ) )
		{
			PreLayout( screen );
		}

		using ( UiPlatform.Scope( "Deferred Deletion" ) )
		{
			RunDeferredDeletion();
		}

		using ( UiPlatform.Scope( "Layout" ) )
		{
			Layout();
		}

		using ( UiPlatform.Scope( "Post Layout" ) )
		{
			PostLayout();
		}

		using ( UiPlatform.Scope( "Deferred Deletion" ) )
		{
			RunDeferredDeletion();
		}
	}

	internal void DirtyAllStyles()
	{
		for ( int i = RootPanels.Count() - 1; i >= 0; i-- )
		{
			if ( !RootPanels[i].IsValid ) continue;
			RootPanels[i].DirtyStylesRecursive();
		}
	}

	//The loops below skip ExternallyDriven roots (editor windows), which tick and lay out themselves

	internal void TickPanels()
	{
		RootPanels.RemoveAll( x => x == null );

		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			if ( RootPanels[i].ExternallyDriven ) continue;
			RootPanels[i].TickInternal();
		}
	}

	internal void PreLayout( Rect screenRect )
	{
		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			if ( RootPanels[i].ExternallyDriven ) continue;
			RootPanels[i].PreLayout( screenRect );
		}
	}

	internal void Layout()
	{
		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			if ( RootPanels[i].ExternallyDriven ) continue;
			RootPanels[i].CalculateLayout();
		}
	}

	internal void PostLayout()
	{
		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			if ( RootPanels[i].ExternallyDriven ) continue;
			RootPanels[i].PostLayout();
		}
	}

	internal void TickInput( bool allowMouseInput )
	{
		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			if ( RootPanels[i].ExternallyDriven ) continue;
			RootPanels[i].TickInputInternal();
		}

		//Tick various input systems. An editor window's tree is not the game's to point at - it has a pointer of its own,
		//and hover-testing the whole editor every frame was work spent finding panels the game must not touch anyway.
		GameInputOrder();
		Input.Tick( inputOrder, allowMouseInput && DoAnyPanelsWantMouseVisible() );

		UiPlatform.TickWorldInput( this );

		//We tick focus here, after the layout. This way any styles that were set at the same time as
		//changing focus will be applied so that when we judge elibility the logic will be correct
		InputFocus.Tick();

		//Send all key events to the focused panel
		InputEventQueue.TickFocused( CurrentFocus );

		//Pass our global input events ( mouse move, double click ) to 2d panels WorldInputs simulate this themselves in WorldInputInternal.Tick
		InputEventQueue.Tick( Input.Hovered, Input.Active );

		//The host's turn: the engine routes the keyboard and mouse between the game and the interface here
		UiPlatform.InputTicked( this );
	}

	//The game's own panels in the order they take the pointer - see GameInputOrder.
	private readonly List<RootPanel> inputOrder = new();

	//Game input panels by layer, highest first, kept between frames; stable insertion sort, no LINQ
	private void GameInputOrder()
	{
		inputOrder.Clear();

		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			var panel = RootPanels[i];
			if ( panel.IsWorldPanel || panel.ExternallyDriven ) continue;

			//A screen panel in a headset is a sheet in front of the head, and the controllers point at it rather than the mouse - see TickWorldInput.
			if ( panel.InHeadset ) continue;

			var layer = panel.ComputedStyle?.ZIndex ?? 0;
			var at = inputOrder.Count;

			while ( at > 0 && ( inputOrder[at - 1].ComputedStyle?.ZIndex ?? 0 ) < layer ) at--;

			inputOrder.Insert( at, panel );
		}
	}

	internal bool DoAnyPanelsWantMouseVisible()
	{
		if ( UiPlatform.PointerForced() is bool forced ) return forced;

		for ( int i = 0; i < RootPanels.Count; i++ )
		{
			if ( !RootPanels[i].IsValid )
				continue;

			if ( !RootPanels[i].IsVisible )
				continue;

			if ( RootPanels[i].IsWorldPanel )
				continue;

			//A sheet in a headset is pointed at with the controllers, not the mouse.
			if ( RootPanels[i].InHeadset )
				continue;

			//An editor window always wants the pointer; that says nothing about whether the game's cursor should be shown
			if ( RootPanels[i].ExternallyDriven )
				continue;

			if ( !RootPanels[i].ChildrenWantMouseInput )
				continue;

			if ( UiPlatform.HidesPointer( RootPanels[i] ) ) continue;

			return true;
		}

		return false;
	}

	//This panel should get deleted at some point
	internal void AddDeferredDeletion( Panel panel )
	{
		Assert.NotNull( panel );

		DeletionList.Add( panel );
	}

	//Delete all panels that were deferred and are no longer playing outro transitions
	internal void RunDeferredDeletion( bool force = false )
	{
		for ( int i = 0; i < DeletionList.Count; i++ )
		{
			var p = DeletionList[i];

			//Hotload can clear the reference; an ancestor can finish deletion before this outro.
			if ( !p.IsValid() )
			{
				DeletionList.RemoveAt( i );
				i--;
				continue;
			}

			if ( !force && p.HasActiveTransitions ) continue;

			try
			{
				p.Delete( true );
			}
			catch ( System.Exception ex )
			{
				Log.Warning( ex, $"Exception while deferred-deleting {p}" );
			}

			DeletionList.RemoveAt( i );
			i--;
		}
	}

	internal void OnHotload()
	{
		for ( int i = 0; i < RootPanels.Count(); i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			RootPanels[i].OnHotloaded();
		}
	}

	internal void OnLanguageChanged()
	{
		for ( int i = 0; i < RootPanels.Count(); i++ )
		{
			if ( !RootPanels[i].IsValid ) continue;
			RootPanels[i].LanguageChanged();
		}
	}

	internal void Clear()
	{
		//Clear any dangling tooltip panel references before destroying the tree.
		TooltipSystem.Clear();

		//Delete immediately on shutdown; deferred deletion needs a next frame that never comes
		foreach ( var rp in RootPanels.ToArray() )
		{
			//Skip editor windows; they belong to their own window, not the game's teardown
			if ( rp.ExternallyDriven ) continue;

			try
			{
				rp.Delete( immediate: true );
			}
			catch ( System.Exception e )
			{
				Log.Warning( e );
			}
		}

		RootPanels.Clear();
		DeletionList.Clear();

		//Drop the entire input subsystem — replaces it wholesale so we don't need to chase
		//individual panel references inside PanelInput, MouseButtonState, InputEventQueue, etc.
		Input = new();
		InputEventQueue = new();
		CurrentFocus = null;
		NextFocus = null;
	}
}
