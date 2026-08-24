using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeTopDownListPanel : ListPanel
{
	public AnimusForgeTopDownListPanel(UIContext context)
		: base(context)
	{
#if BANNERLORD_1_4_OR_GREATER
		StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
#else
		StackLayout.LayoutMethod = LayoutMethod.VerticalBottomToTop;
#endif
	}
}
public sealed class AnimusForgeVersionedScrollableListPanel : ListPanel
{
#if !BANNERLORD_1_4_OR_GREATER
	private bool _childOrderNormalized;
#endif

	public AnimusForgeVersionedScrollableListPanel(UIContext context)
		: base(context)
	{
		StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
	}

	protected override void OnLateUpdate(float dt)
	{
#if !BANNERLORD_1_4_OR_GREATER
		if (!_childOrderNormalized && ChildCount > 0)
		{
			List<Widget> originalOrder = new List<Widget>();
			for (int i = 0; i < ChildCount; i++)
			{
				originalOrder.Add(GetChild(i));
			}
			foreach (Widget child in originalOrder)
			{
				child.SetSiblingIndex(0);
			}
			SetMeasureAndLayoutDirty();
			_childOrderNormalized = true;
		}
#endif
		base.OnLateUpdate(dt);
	}
}
