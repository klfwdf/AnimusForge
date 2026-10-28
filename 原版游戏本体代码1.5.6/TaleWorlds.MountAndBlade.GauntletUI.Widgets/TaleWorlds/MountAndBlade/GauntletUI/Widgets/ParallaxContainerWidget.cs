using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ParallaxContainerWidget(UIContext context) : Widget(context)
{
	private List<ParallaxItemBrushWidget> _parallaxItems = new List<ParallaxItemBrushWidget>();

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		foreach (ParallaxItemBrushWidget parallaxItem in _parallaxItems)
		{
			switch (parallaxItem.InitialDirection)
			{
			}
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		if (child is ParallaxItemBrushWidget item)
		{
			_parallaxItems.Add(item);
		}
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		base.OnBeforeChildRemoved(child);
		if (child is ParallaxItemBrushWidget item)
		{
			_parallaxItems.Remove(item);
		}
	}
}
