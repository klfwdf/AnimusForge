using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class BrushAnimationKeyFrame
{
	public enum ValueType
	{
		Float,
		Color,
		Sprite
	}

	private ValueType _valueType;

	private float _valueAsFloat;

	private Color _valueAsColor;

	private Sprite _valueAsSprite;

	public float Time { get; private set; }

	public int Index { get; private set; }

	public void InitializeAsFloat(float time, float value)
	{
		Time = time;
		_valueType = ValueType.Float;
		_valueAsFloat = value;
	}

	public void InitializeAsColor(float time, Color value)
	{
		Time = time;
		_valueType = ValueType.Color;
		_valueAsColor = value;
	}

	public void InitializeAsSprite(float time, Sprite value)
	{
		Time = time;
		_valueType = ValueType.Sprite;
		_valueAsSprite = value;
	}

	public void InitializeIndex(int index)
	{
		Index = index;
	}

	public float GetValueAsFloat()
	{
		return _valueAsFloat;
	}

	public Color GetValueAsColor()
	{
		return _valueAsColor;
	}

	public Sprite GetValueAsSprite()
	{
		return _valueAsSprite;
	}

	public object GetValueAsObject()
	{
		return _valueType switch
		{
			ValueType.Float => _valueAsFloat, 
			ValueType.Color => _valueAsColor, 
			ValueType.Sprite => _valueAsSprite, 
			_ => null, 
		};
	}

	public BrushAnimationKeyFrame Clone()
	{
		return new BrushAnimationKeyFrame
		{
			_valueType = _valueType,
			_valueAsFloat = _valueAsFloat,
			_valueAsColor = _valueAsColor,
			_valueAsSprite = _valueAsSprite,
			Time = Time,
			Index = Index
		};
	}
}
