using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortPieceTooltipPropertiesListPanel : ListPanel
{
	private bool _isDirty = true;

	public PortPieceTooltipPropertiesListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.ChildCount == 0 || !_isDirty)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < base.ChildCount; i++)
		{
			TextWidget textWidget = base.Children[i].Children[0] as TextWidget;
			TextWidget textWidget2 = base.Children[i].Children[1] as TextWidget;
			float num3 = GetWordWidth(textWidget.Text, textWidget.Brush) * base._scaleToUse + textWidget.ScaledMarginLeft + textWidget.ScaledMarginRight;
			float num4 = GetWordWidth(textWidget2.Text, textWidget2.Brush) * base._scaleToUse + textWidget2.ScaledMarginLeft + textWidget2.ScaledMarginRight;
			if (num < num3)
			{
				num = num3;
			}
			if (num2 < num4)
			{
				num2 = num4;
			}
		}
		float num5 = 0.5f;
		if (num2 > 0f || num > 0f)
		{
			num5 = num2 / (num2 + num);
		}
		for (int j = 0; j < base.ChildCount; j++)
		{
			TextWidget textWidget3 = base.Children[j].Children[0] as TextWidget;
			TextWidget obj = base.Children[j].Children[1] as TextWidget;
			textWidget3.WidthSizePolicy = SizePolicy.StretchToParent;
			obj.WidthSizePolicy = SizePolicy.Fixed;
			obj.ScaledSuggestedWidth = base.Size.X * num5;
			obj.MinWidth = base.Size.X * 1f / 6f * base._inverseScaleToUse;
			obj.MaxWidth = base.Size.X * 2f / 3f * base._inverseScaleToUse;
			if (obj.IsHidden)
			{
				textWidget3.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
				textWidget3.Brush.TextColorFactor = 0.85f;
				textWidget3.MarginRight = 0f;
			}
		}
		_isDirty = false;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		_isDirty = true;
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		_isDirty = true;
	}

	private float GetWordWidth(string word, Brush brush)
	{
		float num = 0f;
		for (int i = 0; i < word.Length; i++)
		{
			num += GetCharacterWidth(word[i], brush);
		}
		return num;
	}

	private float GetCharacterWidth(char character, Brush brush)
	{
		Font mappedFontForLocalization = base.Context.FontFactory.GetMappedFontForLocalization(brush?.Font?.Name);
		float num;
		if (!mappedFontForLocalization.Characters.ContainsKey(character))
		{
			Font font = base.Context.FontFactory.GetUsableFontForCharacter(character) ?? mappedFontForLocalization;
			num = (float)brush.FontSize / (float)font.Size;
			return font.GetCharacterWidth(character, 0.5f) * num;
		}
		num = (float)brush.FontSize / (float)mappedFontForLocalization.Size;
		return mappedFontForLocalization.GetCharacterWidth(character, 0.5f) * num;
	}
}
