using Godot;

namespace CactusTown;

/// <summary>The town Notice Board — walk up and tap to read the current notices.</summary>
public partial class NoticeBoard : TapInteractable
{
	protected override void OnReady() => SetPrompt("Read");
}
