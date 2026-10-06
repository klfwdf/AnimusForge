using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public struct BrushLayerState : IBrushAnimationState, IDataSource
{
	public Color Color;

	public float ColorFactor;

	public float AlphaFactor;

	public float HueFactor;

	public float SaturationFactor;

	public float ValueFactor;

	public float OverlayXOffset;

	public float OverlayYOffset;

	public float XOffset;

	public float YOffset;

	public float Rotation;

	public float ExtendRight;

	public float ExtendTop;

	public float ExtendBottom;

	public float ExtendLeft;

	public Sprite Sprite;

	public void FillFrom(IBrushLayerData styleLayer)
	{
		ColorFactor = styleLayer.ColorFactor;
		AlphaFactor = styleLayer.AlphaFactor;
		HueFactor = styleLayer.HueFactor;
		SaturationFactor = styleLayer.SaturationFactor;
		ValueFactor = styleLayer.ValueFactor;
		Color = styleLayer.Color;
		OverlayXOffset = styleLayer.OverlayXOffset;
		OverlayYOffset = styleLayer.OverlayYOffset;
		XOffset = styleLayer.XOffset;
		YOffset = styleLayer.YOffset;
		Rotation = styleLayer.Rotation;
		ExtendRight = styleLayer.ExtendRight;
		ExtendTop = styleLayer.ExtendTop;
		ExtendBottom = styleLayer.ExtendBottom;
		ExtendLeft = styleLayer.ExtendLeft;
		Sprite = styleLayer.Sprite;
	}

	void IBrushAnimationState.FillFrom(IDataSource source)
	{
		StyleLayer styleLayer = (StyleLayer)source;
		FillFrom(styleLayer);
	}

	void IBrushAnimationState.LerpFrom(IBrushAnimationState start, IDataSource end, float ratio)
	{
		BrushLayerState start2 = (BrushLayerState)(object)start;
		IBrushLayerData end2 = (IBrushLayerData)end;
		LerpFrom(start2, end2, ratio);
	}

	public void LerpFrom(BrushLayerState start, IBrushLayerData end, float ratio)
	{
		ColorFactor = Mathf.Lerp(start.ColorFactor, end.ColorFactor, ratio);
		AlphaFactor = Mathf.Lerp(start.AlphaFactor, end.AlphaFactor, ratio);
		HueFactor = Mathf.Lerp(start.HueFactor, end.HueFactor, ratio);
		SaturationFactor = Mathf.Lerp(start.SaturationFactor, end.SaturationFactor, ratio);
		ValueFactor = Mathf.Lerp(start.ValueFactor, end.ValueFactor, ratio);
		Color = Color.Lerp(start.Color, end.Color, ratio);
		OverlayXOffset = Mathf.Lerp(start.OverlayXOffset, end.OverlayXOffset, ratio);
		OverlayYOffset = Mathf.Lerp(start.OverlayYOffset, end.OverlayYOffset, ratio);
		XOffset = Mathf.Lerp(start.XOffset, end.XOffset, ratio);
		YOffset = Mathf.Lerp(start.YOffset, end.YOffset, ratio);
		Rotation = Mathf.Lerp(start.Rotation, end.Rotation, ratio);
		ExtendRight = Mathf.Lerp(start.ExtendRight, end.ExtendRight, ratio);
		ExtendTop = Mathf.Lerp(start.ExtendTop, end.ExtendTop, ratio);
		ExtendBottom = Mathf.Lerp(start.ExtendBottom, end.ExtendBottom, ratio);
		ExtendLeft = Mathf.Lerp(start.ExtendLeft, end.ExtendLeft, ratio);
		Sprite = ((ratio > 0.9f) ? end.Sprite : start.Sprite);
	}

	public void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
			ColorFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
			AlphaFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
			HueFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
			SaturationFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
			ValueFactor = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
			OverlayXOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
			OverlayYOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			XOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			YOffset = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
			Rotation = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
			ExtendRight = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
			ExtendLeft = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
			ExtendTop = value;
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
			ExtendBottom = value;
			break;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsFloat", 139);
			break;
		}
	}

	public void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
		{
			Color = value;
		}
		else
		{
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsColor", 152);
		}
	}

	public void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Sprite)
		{
			Sprite = value;
		}
		else
		{
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsSprite", 165);
		}
	}

	public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
			return ColorFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
			return AlphaFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
			return HueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
			return SaturationFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
			return ValueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
			return OverlayXOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
			return OverlayYOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			return XOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			return YOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
			return Rotation;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
			return ExtendTop;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
			return ExtendRight;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
			return ExtendBottom;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
			return ExtendLeft;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsFloat", 203);
			return 0f;
		}
	}

	public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
		{
			return Color;
		}
		Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsColor", 215);
		return Color.Black;
	}

	public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Sprite)
		{
			return Sprite;
		}
		Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsSprite", 227);
		return null;
	}

	public static void SetValueAsLerpOfValues(ref BrushLayerState currentState, in BrushAnimationKeyFrame startValue, in BrushAnimationKeyFrame endValue, BrushAnimationProperty.BrushAnimationPropertyType propertyType, float ratio)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
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
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
		case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
			currentState.SetValueAsFloat(propertyType, MathF.Lerp(startValue.GetValueAsFloat(), endValue.GetValueAsFloat(), ratio));
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.Color:
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			currentState.SetValueAsColor(propertyType, Color.Lerp(startValue.GetValueAsColor(), endValue.GetValueAsColor(), ratio));
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
			currentState.SetValueAsSprite(propertyType, ((double)ratio > 0.9) ? endValue.GetValueAsSprite() : startValue.GetValueAsSprite());
			break;
		case BrushAnimationProperty.BrushAnimationPropertyType.IsHidden:
			break;
		}
	}

	void IBrushAnimationState.SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
	{
		SetValueAsColor(propertyType, in value);
	}
}
