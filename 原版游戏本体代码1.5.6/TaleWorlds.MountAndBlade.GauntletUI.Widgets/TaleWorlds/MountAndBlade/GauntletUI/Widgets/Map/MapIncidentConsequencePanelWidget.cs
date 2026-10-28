using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

public class MapIncidentConsequencePanelWidget : CircularAutoScrollablePanelWidget
{
	private float _initialHeight;

	private bool _isFirstFrame = true;

	private Widget _previousActiveOption;

	public Widget OptionsList { get; set; }

	public float AnimationSpeed { get; set; } = 10f;

	public MapIncidentConsequencePanelWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isFirstFrame)
		{
			_initialHeight = base.SuggestedHeight;
			_isFirstFrame = false;
		}
		Widget activeOption = GetActiveOption();
		if (activeOption != _previousActiveOption)
		{
			_previousActiveOption = activeOption;
			return;
		}
		float num = base.InnerPanel.Size.Y * base._inverseScaleToUse + base.ClipRect.MarginTop + base.ClipRect.MarginBottom;
		float maxValue = base.ParentWidget.Size.Y * base._inverseScaleToUse;
		int num2;
		if (num > _initialHeight)
		{
			if (base.IsHovered)
			{
				num2 = 1;
				goto IL_00b2;
			}
			if (activeOption == null)
			{
				num2 = 0;
			}
			else
			{
				num2 = (activeOption.IsHovered ? 1 : 0);
				if (num2 != 0)
				{
					goto IL_00b2;
				}
			}
		}
		else
		{
			num2 = 0;
		}
		float num3 = _initialHeight;
		goto IL_00bf;
		IL_00b2:
		num3 = MathF.Clamp(num, _initialHeight, maxValue);
		goto IL_00bf;
		IL_00bf:
		float num4 = num3;
		if (num2 != 0 && num4 == num)
		{
			StopScrolling();
		}
		base.SuggestedHeight = MathF.Lerp(base.SuggestedHeight, num4, MathF.Min(AnimationSpeed * dt, 1f), 0.01f);
	}

	private Widget GetActiveOption()
	{
		Widget result = null;
		for (int i = 0; i < OptionsList.ChildCount; i++)
		{
			Widget widget = OptionsList.Children[i];
			if (widget.IsHovered)
			{
				return widget;
			}
			if (widget is ButtonWidget { IsSelected: not false })
			{
				result = widget;
			}
		}
		return result;
	}
}
