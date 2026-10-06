using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class BrushLayerAnimation
{
	private MBList<BrushAnimationProperty> _collections;

	public string LayerName { get; set; }

	public MBReadOnlyList<BrushAnimationProperty> Collections => _collections;

	public BrushLayerAnimation()
	{
		LayerName = null;
		_collections = new MBList<BrushAnimationProperty>();
	}

	internal void RemoveAnimationProperty(BrushAnimationProperty property)
	{
		_collections.Remove(property);
	}

	public void AddAnimationProperty(BrushAnimationProperty property)
	{
		_collections.Add(property);
	}

	private void FillFrom(BrushLayerAnimation brushLayerAnimation)
	{
		LayerName = brushLayerAnimation.LayerName;
		_collections = new MBList<BrushAnimationProperty>();
		foreach (BrushAnimationProperty collection in brushLayerAnimation._collections)
		{
			BrushAnimationProperty item = collection.Clone();
			_collections.Add(item);
		}
	}

	public BrushLayerAnimation Clone()
	{
		BrushLayerAnimation brushLayerAnimation = new BrushLayerAnimation();
		brushLayerAnimation.FillFrom(this);
		return brushLayerAnimation;
	}
}
