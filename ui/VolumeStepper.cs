using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// A 0–10 volume control: [−] button, ten tappable segments, [+] button. Tap a
/// segment to snap to it. Emits <see cref="LevelChanged"/> (0–10).
/// </summary>
public partial class VolumeStepper : HBoxContainer
{
	[Signal] public delegate void LevelChangedEventHandler(int level);

	public const int Steps = 10;

	private int _level = 7;
	private readonly List<Panel> _segments = new();

	public override void _Ready()
	{
		AddThemeConstantOverride("separation", 6);
		Alignment = AlignmentMode.Center;

		AddChild(MakeStepButton("−", -1));
		for (var i = 0; i < Steps; i++)
			AddChild(MakeSegment(i));
		AddChild(MakeStepButton("+", 1));

		Repaint();
	}

	public void SetLevelSilent(int level)
	{
		_level = Mathf.Clamp(level, 0, Steps);
		Repaint();
	}

	private Button MakeStepButton(string text, int delta)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(66, 52) };
		button.AddThemeFontSizeOverride("font_size", 34);
		button.Pressed += () => Apply(_level + delta);
		return button;
	}

	private Panel MakeSegment(int index)
	{
		var panel = new Panel { CustomMinimumSize = new Vector2(40, 52), MouseFilter = MouseFilterEnum.Stop };
		panel.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
				or InputEventScreenTouch { Pressed: true })
				Apply(index + 1);
		};
		_segments.Add(panel);
		return panel;
	}

	private void Apply(int level)
	{
		_level = Mathf.Clamp(level, 0, Steps);
		Repaint();
		EmitSignal(SignalName.LevelChanged, _level);
	}

	private void Repaint()
	{
		for (var i = 0; i < _segments.Count; i++)
		{
			var style = new StyleBoxFlat
			{
				BgColor = i < _level ? new Color(0.49f, 0.78f, 0.55f) : new Color(0.28f, 0.32f, 0.30f),
				CornerRadiusTopLeft = 4,
				CornerRadiusTopRight = 4,
				CornerRadiusBottomLeft = 4,
				CornerRadiusBottomRight = 4,
			};
			_segments[i].AddThemeStyleboxOverride("panel", style);
		}
	}
}
