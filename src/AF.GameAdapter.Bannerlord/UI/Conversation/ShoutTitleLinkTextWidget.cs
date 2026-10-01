using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace AnimusForge;

public class ShoutTitleLinkTextWidget : RichTextWidget
{
	private static readonly Color DefaultTitleColor = new Color(1f, 1f, 1f, 1f);

	private static readonly Color HoverTitleColor = new Color(1f, 0.84f, 0f, 1f);

	private bool _isHighlightApplied;

	public ShoutTitleLinkTextWidget(UIContext context)
		: base(context)
	{
	}

	public override void SetState(string stateName)
	{
		base.SetState(stateName);
		ApplyHighlight(string.Equals(stateName, "Hovered", StringComparison.OrdinalIgnoreCase));
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		ApplyHighlight(true);
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		ApplyHighlight(false);
	}

	private void ApplyHighlight(bool isHighlighted)
	{
		if (_isHighlightApplied == isHighlighted)
		{
			return;
		}
		_isHighlightApplied = isHighlighted;
		Brush.FontColor = isHighlighted ? HoverTitleColor : DefaultTitleColor;
		RegisterUpdateBrushes();
	}
}
