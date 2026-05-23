using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerArmoryCosmeticsSectionWidget : Widget
{
	private float _tauntAssignmentStateTimer;

	private bool _isTauntAssignmentActive;

	private float _tauntAssignmentStateAnimationDuration;

	private float _tauntAssignmentStateAlpha;

	private Widget _topSectionParent;

	private Widget _bottomSectionParent;

	private Widget _sortControlsParent;

	private Widget _categorySeparatorWidget;

	public bool IsTauntAssignmentActive
	{
		get
		{
			return _isTauntAssignmentActive;
		}
		set
		{
			if (value != _isTauntAssignmentActive)
			{
				_isTauntAssignmentActive = value;
				OnPropertyChanged(value, "IsTauntAssignmentActive");
				_tauntAssignmentStateTimer = 0f;
				base.EventManager.AddLateUpdateAction(this, AnimateTauntAssignmentStates, 1);
			}
		}
	}

	public float TauntAssignmentStateAnimationDuration
	{
		get
		{
			return _tauntAssignmentStateAnimationDuration;
		}
		set
		{
			if (value != _tauntAssignmentStateAnimationDuration)
			{
				_tauntAssignmentStateAnimationDuration = value;
				OnPropertyChanged(value, "TauntAssignmentStateAnimationDuration");
			}
		}
	}

	public float TauntAssignmentStateAlpha
	{
		get
		{
			return _tauntAssignmentStateAlpha;
		}
		set
		{
			if (value != _tauntAssignmentStateAlpha)
			{
				_tauntAssignmentStateAlpha = value;
				OnPropertyChanged(value, "TauntAssignmentStateAlpha");
			}
		}
	}

	public Widget TopSectionParent
	{
		get
		{
			return _topSectionParent;
		}
		set
		{
			if (value != _topSectionParent)
			{
				_topSectionParent = value;
				OnPropertyChanged(value, "TopSectionParent");
			}
		}
	}

	public Widget BottomSectionParent
	{
		get
		{
			return _bottomSectionParent;
		}
		set
		{
			if (value != _bottomSectionParent)
			{
				_bottomSectionParent = value;
				OnPropertyChanged(value, "BottomSectionParent");
			}
		}
	}

	public Widget SortControlsParent
	{
		get
		{
			return _sortControlsParent;
		}
		set
		{
			if (value != _sortControlsParent)
			{
				_sortControlsParent = value;
				OnPropertyChanged(value, "SortControlsParent");
			}
		}
	}

	public Widget CategorySeparatorWidget
	{
		get
		{
			return _categorySeparatorWidget;
		}
		set
		{
			if (value != _categorySeparatorWidget)
			{
				_categorySeparatorWidget = value;
				OnPropertyChanged(value, "CategorySeparatorWidget");
			}
		}
	}

	public MultiplayerArmoryCosmeticsSectionWidget(UIContext context)
		: base(context)
	{
	}

	private void AnimateTauntAssignmentStates(float dt)
	{
		_tauntAssignmentStateTimer += dt;
		float amount;
		if (_tauntAssignmentStateTimer < TauntAssignmentStateAnimationDuration)
		{
			amount = _tauntAssignmentStateTimer / TauntAssignmentStateAnimationDuration;
			base.EventManager.AddLateUpdateAction(this, AnimateTauntAssignmentStates, 1);
		}
		else
		{
			amount = 1f;
		}
		float valueFrom = (IsTauntAssignmentActive ? 1f : TauntAssignmentStateAlpha);
		float valueTo = (IsTauntAssignmentActive ? TauntAssignmentStateAlpha : 1f);
		float alpha = MathF.Lerp(valueFrom, valueTo, amount);
		SetWidgetAlpha(TopSectionParent, alpha);
		SetWidgetAlpha(BottomSectionParent, alpha);
		SetWidgetAlpha(SortControlsParent, alpha);
		SetWidgetAlpha(CategorySeparatorWidget, alpha);
	}

	private void SetWidgetAlpha(Widget widget, float alpha)
	{
		if (widget != null)
		{
			widget.IsVisible = alpha != 0f;
			widget.SetGlobalAlphaRecursively(alpha);
		}
	}
}
