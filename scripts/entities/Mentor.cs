using Godot;

namespace CactusTown;

/// <summary>
/// Sage — the elder cactus who greets the player in Main Square and also waits
/// on the House hub. Tap to hear a progress-aware tip. In town Sage also owns
/// the intro "welcome sign" repair (a plain <see cref="RepairableObject"/>
/// nearby with ObjectId "intro_sign").
/// </summary>
public partial class Mentor : TapInteractable
{
	protected override void OnReady()
	{
		SetPrompt("Talk to Sage");
		GetNodeOrNull<NpcPlant>("Body/Npc")?.Randomize("sage-the-mentor");
	}
}
