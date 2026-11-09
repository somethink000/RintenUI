namespace Rinten.UI;

/// <summary>What the panel walk asks of the device it builds batches for.</summary>
public interface IUiDevice
{
	/// <summary>Slots in the device's indexed table of textures; nought where every batch binds its own.</summary>
	int BindlessSlots { get; }

	/// <summary>Moves when the device loses or remakes its resources, so nothing kept from before is used.</summary>
	int ResourceGeneration { get; }

	/// <summary>The slot a texture handle sits in, in the indexed table.</summary>
	uint TextureIndex( ulong handle );
}
