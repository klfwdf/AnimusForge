using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.GauntletUI;

public class BrushAnimationProperty
{
	public enum BrushAnimationPropertyType
	{
		Name,
		ColorFactor,
		Color,
		AlphaFactor,
		HueFactor,
		SaturationFactor,
		ValueFactor,
		FontColor,
		OverlayXOffset,
		OverlayYOffset,
		TextGlowColor,
		TextOutlineColor,
		TextOutlineAmount,
		TextGlowRadius,
		TextBlur,
		TextShadowOffset,
		TextShadowAngle,
		TextColorFactor,
		TextAlphaFactor,
		TextHueFactor,
		TextSaturationFactor,
		TextValueFactor,
		Sprite,
		IsHidden,
		XOffset,
		YOffset,
		Rotation,
		OverridenWidth,
		OverridenHeight,
		WidthPolicy,
		HeightPolicy,
		HorizontalFlip,
		VerticalFlip,
		OverlayMethod,
		OverlaySprite,
		ExtendLeft,
		ExtendRight,
		ExtendTop,
		ExtendBottom,
		UseRandomBaseOverlayXOffset,
		UseRandomBaseOverlayYOffset,
		Font,
		FontStyle,
		FontSize
	}

	public BrushAnimationPropertyType PropertyType;

	private List<BrushAnimationKeyFrame> _keyFrames;

	public string LayerName { get; set; }

	public IEnumerable<BrushAnimationKeyFrame> KeyFrames => _keyFrames.AsReadOnly();

	public int Count => _keyFrames.Count;

	public BrushAnimationProperty()
	{
		_keyFrames = new List<BrushAnimationKeyFrame>();
	}

	public BrushAnimationKeyFrame GetFrameAfter(float time)
	{
		for (int i = 0; i < _keyFrames.Count; i++)
		{
			BrushAnimationKeyFrame brushAnimationKeyFrame = _keyFrames[i];
			if (time < brushAnimationKeyFrame.Time)
			{
				return brushAnimationKeyFrame;
			}
		}
		return null;
	}

	public BrushAnimationKeyFrame GetFrameAt(int i)
	{
		if (i >= 0 && i < _keyFrames.Count)
		{
			return _keyFrames[i];
		}
		return null;
	}

	public BrushAnimationProperty Clone()
	{
		BrushAnimationProperty brushAnimationProperty = new BrushAnimationProperty();
		brushAnimationProperty.FillFrom(this);
		return brushAnimationProperty;
	}

	private void FillFrom(BrushAnimationProperty collection)
	{
		PropertyType = collection.PropertyType;
		LayerName = collection.LayerName;
		_keyFrames = new List<BrushAnimationKeyFrame>(collection._keyFrames.Count);
		for (int i = 0; i < collection._keyFrames.Count; i++)
		{
			BrushAnimationKeyFrame item = collection._keyFrames[i].Clone();
			_keyFrames.Add(item);
		}
	}

	public void AddKeyFrame(BrushAnimationKeyFrame keyFrame)
	{
		_keyFrames.Add(keyFrame);
		_keyFrames = _keyFrames.OrderBy((BrushAnimationKeyFrame k) => k.Time).ToList();
		for (int num = 0; num < _keyFrames.Count; num++)
		{
			_keyFrames[num].InitializeIndex(num);
		}
	}

	public void RemoveKeyFrame(BrushAnimationKeyFrame keyFrame)
	{
		_keyFrames.Remove(keyFrame);
	}
}
