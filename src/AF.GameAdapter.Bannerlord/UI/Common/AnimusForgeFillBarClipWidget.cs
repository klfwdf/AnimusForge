using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

public sealed class AnimusForgeFillBarClipWidget : Widget
{
	private float _fillPercent = 100f;

	private float _fullWidth = 100f;

	public AnimusForgeFillBarClipWidget(UIContext context)
		: base(context)
	{
		ClipContents = true;
	}

	public float FillPercent
	{
		get => _fillPercent;
		set
		{
			_fillPercent = Math.Max(0f, Math.Min(100f, value));
			ApplyWidthIfNeeded();
		}
	}

	public float FullWidth
	{
		get => _fullWidth;
		set
		{
			_fullWidth = Math.Max(0f, value);
			ApplyWidthIfNeeded();
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		ApplyWidthIfNeeded();
	}

	private void ApplyWidthIfNeeded()
	{
		float fullWidth = Math.Max(0f, _fullWidth);
		float fillWidth = fullWidth * _fillPercent / 100f;
		WidthSizePolicy = SizePolicy.Fixed;
		SuggestedWidth = fullWidth;
		HorizontalAlignment = HorizontalAlignment.Left;
		PositionXOffset = 0f;
		if (ChildCount <= 0)
		{
			return;
		}
		Widget clipWidget = GetChild(0);
		if (clipWidget == null)
		{
			return;
		}
		clipWidget.ClipContents = true;
		clipWidget.WidthSizePolicy = SizePolicy.Fixed;
		clipWidget.SuggestedWidth = fillWidth;
		clipWidget.HorizontalAlignment = HorizontalAlignment.Left;
		clipWidget.PositionXOffset = 0f;
		if (clipWidget.ChildCount <= 0)
		{
			return;
		}
		Widget fillWidget = clipWidget.GetChild(0);
		if (fillWidget == null)
		{
			return;
		}
		fillWidget.WidthSizePolicy = SizePolicy.Fixed;
		fillWidget.SuggestedWidth = fullWidth;
		fillWidget.HorizontalAlignment = HorizontalAlignment.Left;
		fillWidget.PositionXOffset = 0f;
	}
}
