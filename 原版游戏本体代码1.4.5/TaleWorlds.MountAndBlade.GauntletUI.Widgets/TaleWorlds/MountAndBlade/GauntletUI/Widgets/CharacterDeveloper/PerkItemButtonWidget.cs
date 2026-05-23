using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class PerkItemButtonWidget : ButtonWidget
{
	public enum AnimState
	{
		Idle,
		Start,
		Starting,
		Playing
	}

	private AnimState _animState;

	private int _tickCount;

	private bool _isSelectable;

	private int _level;

	private int _alternativeType;

	private int _perkState = -1;

	private Widget _perkVisualWidget;

	public Brush NotEarnedPerkBrush { get; set; }

	public Brush EarnedNotSelectedPerkBrush { get; set; }

	public Brush EarnedActivePerkBrush { get; set; }

	public Brush EarnedNotActivePerkBrush { get; set; }

	public Brush EarnedPreviousPerkNotSelectedPerkBrush { get; set; }

	public BrushWidget PerkVisualWidgetParent { get; set; }

	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (_level != value)
			{
				_level = value;
				OnPropertyChanged(value, "Level");
			}
		}
	}

	public Widget PerkVisualWidget
	{
		get
		{
			return _perkVisualWidget;
		}
		set
		{
			if (_perkVisualWidget != value)
			{
				_perkVisualWidget = value;
				OnPropertyChanged(value, "PerkVisualWidget");
			}
		}
	}

	public int PerkState
	{
		get
		{
			return _perkState;
		}
		set
		{
			if (_perkState != value)
			{
				_perkState = value;
				OnPropertyChanged(value, "PerkState");
				UpdatePerkStateVisual(PerkState);
			}
		}
	}

	public int AlternativeType
	{
		get
		{
			return _alternativeType;
		}
		set
		{
			if (_alternativeType != value)
			{
				_alternativeType = value;
				OnPropertyChanged(value, "AlternativeType");
			}
		}
	}

	public PerkItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (PerkVisualWidget != null && ((PerkVisualWidget.Sprite != null && base.Context.SpriteData.GetSprite(PerkVisualWidget.Sprite.Name) == null) || PerkVisualWidget.Sprite == null))
		{
			PerkVisualWidget.Sprite = base.Context.SpriteData.GetSprite("SPPerks\\locked_fallback");
		}
		if (_animState == AnimState.Start)
		{
			_tickCount++;
			if (_tickCount > 20)
			{
				_animState = AnimState.Starting;
			}
		}
		else if (_animState == AnimState.Starting)
		{
			PerkVisualWidgetParent?.BrushRenderer.RestartAnimation();
			_animState = AnimState.Playing;
		}
	}

	private void SetColorState(bool isActive)
	{
		if (PerkVisualWidget == null)
		{
			return;
		}
		float alphaFactor = (isActive ? 1f : 1f);
		float colorFactor = (isActive ? 1.3f : 0.75f);
		List<BrushWidget> list = FindChildrenWithType<BrushWidget>();
		for (int i = 0; i < list.Count; i++)
		{
			foreach (Style style in list[i].Brush.Styles)
			{
				for (int j = 0; j < style.LayerCount; j++)
				{
					StyleLayer layer = style.GetLayer(j);
					layer.AlphaFactor = alphaFactor;
					layer.ColorFactor = colorFactor;
				}
			}
		}
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (_isSelectable)
		{
			base.Context.TwoDimensionContext.PlaySound("popup");
		}
	}

	private void UpdatePerkStateVisual(int perkState)
	{
		if (PerkVisualWidgetParent != null)
		{
			switch (perkState)
			{
			case 0:
				PerkVisualWidgetParent.Brush = NotEarnedPerkBrush;
				_isSelectable = false;
				break;
			case 1:
				PerkVisualWidgetParent.Brush = EarnedNotSelectedPerkBrush;
				_animState = AnimState.Start;
				_isSelectable = true;
				_ = PerkVisualWidgetParent.Brush.TransitionDuration;
				break;
			case 2:
				PerkVisualWidgetParent.Brush = EarnedActivePerkBrush;
				_isSelectable = false;
				break;
			case 3:
				PerkVisualWidgetParent.Brush = EarnedNotActivePerkBrush;
				_isSelectable = false;
				break;
			case 4:
				PerkVisualWidgetParent.Brush = EarnedPreviousPerkNotSelectedPerkBrush;
				_isSelectable = false;
				break;
			default:
				Debug.FailedAssert("Perk visual state is not defined", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\CharacterDeveloper\\PerkItemButtonWidget.cs", "UpdatePerkStateVisual", 132);
				break;
			}
		}
	}
}
