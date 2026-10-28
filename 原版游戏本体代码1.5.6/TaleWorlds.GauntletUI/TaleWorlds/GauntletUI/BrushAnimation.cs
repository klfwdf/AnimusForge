using System.Collections.Generic;

namespace TaleWorlds.GauntletUI;

public class BrushAnimation
{
	private Dictionary<string, BrushLayerAnimation> _data;

	public string Name { get; set; }

	public float Duration { get; set; }

	public bool Loop { get; set; }

	public AnimationInterpolation.Type InterpolationType { get; set; }

	public AnimationInterpolation.Function InterpolationFunction { get; set; }

	public BrushLayerAnimation StyleAnimation { get; set; }

	public BrushAnimation()
	{
		_data = new Dictionary<string, BrushLayerAnimation>();
	}

	public void AddAnimationProperty(BrushAnimationProperty property)
	{
		BrushLayerAnimation value = null;
		if (string.IsNullOrEmpty(property.LayerName))
		{
			if (StyleAnimation == null)
			{
				StyleAnimation = new BrushLayerAnimation();
			}
			value = StyleAnimation;
		}
		else if (!_data.TryGetValue(property.LayerName, out value))
		{
			value = new BrushLayerAnimation();
			value.LayerName = property.LayerName;
			_data.Add(property.LayerName, value);
		}
		value.AddAnimationProperty(property);
	}

	public void RemoveAnimationProperty(BrushAnimationProperty property)
	{
		BrushLayerAnimation brushLayerAnimation = null;
		if (string.IsNullOrEmpty(property.LayerName))
		{
			if (StyleAnimation == null)
			{
				StyleAnimation = new BrushLayerAnimation();
			}
			brushLayerAnimation = StyleAnimation;
		}
		else
		{
			if (!_data.ContainsKey(property.LayerName))
			{
				return;
			}
			brushLayerAnimation = _data[property.LayerName];
		}
		brushLayerAnimation.RemoveAnimationProperty(property);
		if (brushLayerAnimation.Collections.Count == 0)
		{
			_data.Remove(property.LayerName);
		}
	}

	public void FillFrom(BrushAnimation animation)
	{
		Name = animation.Name;
		Duration = animation.Duration;
		Loop = animation.Loop;
		InterpolationType = animation.InterpolationType;
		InterpolationFunction = animation.InterpolationFunction;
		if (animation.StyleAnimation != null)
		{
			StyleAnimation = animation.StyleAnimation.Clone();
		}
		_data = new Dictionary<string, BrushLayerAnimation>();
		foreach (KeyValuePair<string, BrushLayerAnimation> datum in animation._data)
		{
			string key = datum.Key;
			BrushLayerAnimation value = datum.Value.Clone();
			_data.Add(key, value);
		}
	}

	public BrushLayerAnimation GetLayerAnimation(string name)
	{
		if (_data.ContainsKey(name))
		{
			return _data[name];
		}
		return null;
	}

	public IEnumerable<BrushLayerAnimation> GetLayerAnimations()
	{
		return _data.Values;
	}
}
