using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class RainbowRichTextWidget(UIContext context) : RichTextWidget(context)
{
	private Color targetColor = Color.White;

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.Brush.FontColor = Color.Lerp(base.ReadOnlyBrush.FontColor, targetColor, dt);
		if (base.Brush.FontColor.ToVec3().Distance(targetColor.ToVec3()) < 1f)
		{
			Random random = new Random();
			targetColor = Color.FromVector3(new Vector3(random.Next(255), random.Next(255), random.Next(255)));
		}
	}
}
