using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardStatsParentWidget : Widget
{
	private bool _isActive;

	private float _activeAlpha;

	private float _inactiveAlpha;

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
				RefreshActiveState();
			}
		}
	}

	public bool IsInactive
	{
		get
		{
			return !IsActive;
		}
		set
		{
			if (value == IsActive)
			{
				IsActive = !value;
				OnPropertyChanged(value, "IsInactive");
			}
		}
	}

	public float ActiveAlpha
	{
		get
		{
			return _activeAlpha;
		}
		set
		{
			if (value != _activeAlpha)
			{
				_activeAlpha = value;
				OnPropertyChanged(value, "ActiveAlpha");
			}
		}
	}

	public float InactiveAlpha
	{
		get
		{
			return _inactiveAlpha;
		}
		set
		{
			if (value != _inactiveAlpha)
			{
				_inactiveAlpha = value;
				OnPropertyChanged(value, "InactiveAlpha");
			}
		}
	}

	public MultiplayerScoreboardStatsParentWidget(UIContext context)
		: base(context)
	{
	}

	private void RefreshActiveState()
	{
		float alphaFactor = (IsActive ? ActiveAlpha : InactiveAlpha);
		List<Widget> allChildrenRecursive = GetAllChildrenRecursive();
		for (int i = 0; i < allChildrenRecursive.Count; i++)
		{
			if (allChildrenRecursive[i] is RichTextWidget widget)
			{
				widget.SetAlpha(alphaFactor);
			}
			else if (allChildrenRecursive[i] is TextWidget widget2)
			{
				widget2.SetAlpha(alphaFactor);
			}
		}
	}
}
