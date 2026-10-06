using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public struct BrushState : IBrushAnimationState, IDataSource
{
	public Color FontColor;

	public Color TextGlowColor;

	public Color TextOutlineColor;

	public float TextOutlineAmount;

	public float TextGlowRadius;

	public float TextBlur;

	public float TextShadowOffset;

	public float TextShadowAngle;

	public float TextColorFactor;

	public float TextAlphaFactor;

	public float TextHueFactor;

	public float TextSaturationFactor;

	public float TextValueFactor;

	public float XOffset;

	public float YOffset;

	public void FillFrom(Style style)
	{
		FontColor = style.FontColor;
		TextGlowColor = style.TextGlowColor;
		TextOutlineColor = style.TextOutlineColor;
		TextOutlineAmount = style.TextOutlineAmount;
		TextGlowRadius = style.TextGlowRadius;
		TextBlur = style.TextBlur;
		TextShadowOffset = style.TextShadowOffset;
		TextShadowAngle = style.TextShadowAngle;
		TextColorFactor = style.TextColorFactor;
		TextAlphaFactor = style.TextAlphaFactor;
		TextHueFactor = style.TextHueFactor;
		TextSaturationFactor = style.TextSaturationFactor;
		TextValueFactor = style.TextValueFactor;
		XOffset = style.XOffset;
		YOffset = style.YOffset;
	}

	public void LerpFrom(BrushState start, Style end, float ratio)
	{
		FontColor = Color.Lerp(start.FontColor, end.FontColor, ratio);
		TextGlowColor = Color.Lerp(start.TextGlowColor, end.TextGlowColor, ratio);
		TextOutlineColor = Color.Lerp(start.TextOutlineColor, end.TextOutlineColor, ratio);
		TextOutlineAmount = Mathf.Lerp(start.TextOutlineAmount, end.TextOutlineAmount, ratio);
		TextGlowRadius = Mathf.Lerp(start.TextGlowRadius, end.TextGlowRadius, ratio);
		TextBlur = Mathf.Lerp(start.TextBlur, end.TextBlur, ratio);
		TextShadowOffset = Mathf.Lerp(start.TextShadowOffset, end.TextShadowOffset, ratio);
		TextShadowAngle = Mathf.Lerp(start.TextShadowAngle, end.TextShadowAngle, ratio);
		TextColorFactor = Mathf.Lerp(start.TextColorFactor, end.TextColorFactor, ratio);
		TextAlphaFactor = Mathf.Lerp(start.TextAlphaFactor, end.TextAlphaFactor, ratio);
		TextHueFactor = Mathf.Lerp(start.TextHueFactor, end.TextHueFactor, ratio);
		TextSaturationFactor = Mathf.Lerp(start.TextSaturationFactor, end.TextSaturationFactor, ratio);
		TextValueFactor = Mathf.Lerp(start.TextValueFactor, end.TextValueFactor, ratio);
		XOffset = Mathf.Lerp(start.XOffset, end.XOffset, ratio);
		YOffset = Mathf.Lerp(start.YOffset, end.YOffset, ratio);
	}

	void IBrushAnimationState.FillFrom(IDataSource source)
	{
		Style style = (Style)source;
		FillFrom(style);
	}

	void IBrushAnimationState.LerpFrom(IBrushAnimationState start, IDataSource end, float ratio)
	{
		BrushState start2 = (BrushState)(object)start;
		Style end2 = (Style)end;
		LerpFrom(start2, end2, ratio);
	}

	public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
			return TextOutlineAmount;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
			return TextGlowRadius;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
			return TextBlur;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
			return TextShadowOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
			return TextShadowAngle;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
			return TextColorFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
			return TextAlphaFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
			return TextHueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
			return TextSaturationFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			return TextValueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			return XOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			return YOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsFloat", 112);
			return 0f;
		default:
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsFloat", 116);
			return 0f;
		}
	}

	public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			return FontColor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			return TextGlowColor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			return TextOutlineColor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsColor", 142);
			return Color.Black;
		default:
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsColor", 145);
			return Color.Black;
		}
	}

	public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.FontColor || (uint)(propertyType - 10) <= 11u)
		{
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsSprite", 167);
			return null;
		}
		Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsSprite", 171);
		return null;
	}

	public void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
			TextOutlineAmount = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
			TextGlowRadius = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
			TextBlur = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
			TextShadowOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
			TextShadowAngle = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
			TextColorFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
			TextAlphaFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
			TextHueFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
			TextSaturationFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			TextValueFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			XOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			YOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsFloat", 220);
			break;
		default:
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsFloat", 224);
			break;
		}
	}

	public void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			FontColor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			TextGlowColor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			TextOutlineColor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsColor", 253);
			break;
		default:
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsColor", 256);
			break;
		}
	}

	public void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.FontColor || (uint)(propertyType - 10) <= 11u)
		{
			Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsSprite", 278);
		}
		else
		{
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsSprite", 281);
		}
	}

	public TextMaterial CreateTextMaterial(TwoDimensionDrawContext drawContext)
	{
		TextMaterial textMaterial = drawContext.CreateTextMaterial();
		textMaterial.Color = FontColor;
		textMaterial.GlowColor = TextGlowColor;
		textMaterial.OutlineColor = TextOutlineColor;
		textMaterial.OutlineAmount = TextOutlineAmount;
		textMaterial.GlowRadius = TextGlowRadius;
		textMaterial.Blur = TextBlur;
		textMaterial.ShadowOffset = TextShadowOffset;
		textMaterial.ShadowAngle = TextShadowAngle;
		textMaterial.ColorFactor = TextColorFactor;
		textMaterial.AlphaFactor = TextAlphaFactor;
		textMaterial.HueFactor = TextHueFactor;
		textMaterial.SaturationFactor = TextSaturationFactor;
		textMaterial.ValueFactor = TextValueFactor;
		return textMaterial;
	}

	void IBrushAnimationState.SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
	{
		SetValueAsColor(propertyType, in value);
	}
}
