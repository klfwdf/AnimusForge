using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ParallaxContainerWidget : Widget
{
	private List<ParallaxItemBrushWidget> _parallaxItems = new List<ParallaxItemBrushWidget>();

	public ParallaxContainerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		using List<ParallaxItemBrushWidget>.Enumerator enumerator = _parallaxItems.GetEnumerator();
		while (enumerator.MoveNext())
		{
			switch (enumerator.Current.InitialDirection)
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
