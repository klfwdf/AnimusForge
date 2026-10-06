using System;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ScreenBackgroundBrushWidget : BrushWidget
{
	private bool _firstFrame = true;

	private float _totalSmokeXOffset;

	private float _totalParticleXOffset;

	public bool IsParticleVisible { get; set; }

	public bool IsSmokeVisible { get; set; }

	public bool IsFullscreenImageEnabled { get; set; }

	public bool AnimEnabled { get; set; }

	public Widget ParticleWidget1 { get; set; }

	public Widget ParticleWidget2 { get; set; }

	public Widget SmokeWidget1 { get; set; }

	public Widget SmokeWidget2 { get; set; }

	public float SmokeSpeedModifier { get; set; } = 1f;

	public float ParticleSpeedModifier { get; set; } = 1f;

	public ScreenBackgroundBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_firstFrame)
		{
			UpdateBackgroundImage();
			_firstFrame = false;
		}
		ParticleWidget1.IsVisible = IsParticleVisible;
		ParticleWidget2.IsVisible = IsParticleVisible;
		SmokeWidget1.IsVisible = IsSmokeVisible;
		SmokeWidget2.IsVisible = IsSmokeVisible;
		if (!AnimEnabled)
		{
			return;
		}
		if (IsParticleVisible)
		{
			ParticleWidget1.PositionXOffset = _totalParticleXOffset;
			ParticleWidget2.PositionXOffset = ParticleWidget1.PositionXOffset + ParticleWidget1.SuggestedWidth;
			_totalParticleXOffset -= dt * 10f * ParticleSpeedModifier;
			if (Math.Abs(_totalParticleXOffset) >= ParticleWidget1.SuggestedWidth)
			{
				_totalParticleXOffset = 0f;
			}
		}
		if (IsSmokeVisible)
		{
			SmokeWidget1.PositionXOffset = _totalSmokeXOffset;
			SmokeWidget2.PositionXOffset = SmokeWidget1.PositionXOffset - SmokeWidget1.SuggestedWidth;
			if (Math.Abs(_totalSmokeXOffset) >= SmokeWidget1.SuggestedWidth)
			{
				_totalSmokeXOffset = 0f;
			}
			_totalSmokeXOffset += dt * 10f * SmokeSpeedModifier;
		}
	}

	private void UpdateBackgroundImage()
	{
		if (IsFullscreenImageEnabled)
		{
			int index = base.Context.UIRandom.Next(base.Brush.Styles.Count);
			StyleLayer[] layers = base.ReadOnlyBrush.Styles.ElementAt(index).GetLayers();
			if (layers.Length != 0)
			{
				base.Brush.Sprite = layers[0].Sprite;
			}
		}
		else
		{
			base.Brush.Sprite = null;
		}
	}
}
