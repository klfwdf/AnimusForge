using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameMenu;

public class GameMenuTroopSelectionItemButtonWidget : ButtonWidget
{
	private bool _initialized;

	private bool _isDirty = true;

	private int _maxAmount;

	private int _currentAmount;

	private bool _isRosterFull;

	private bool _isLocked;

	private bool _isTroopHero;

	public ButtonWidget AddButtonWidget { get; set; }

	public ButtonWidget RemoveButtonWidget { get; set; }

	public Widget CheckmarkVisualWidget { get; set; }

	public Widget AddRemoveControls { get; set; }

	public Widget HeroHealthParent { get; set; }

	public bool IsRosterFull
	{
		get
		{
			return _isRosterFull;
		}
		set
		{
			if (_isRosterFull != value)
			{
				_isRosterFull = value;
				OnPropertyChanged(value, "IsRosterFull");
				_isDirty = true;
			}
		}
	}

	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (_isLocked != value)
			{
				_isLocked = value;
				OnPropertyChanged(value, "IsLocked");
				_isDirty = true;
			}
		}
	}

	public bool IsTroopHero
	{
		get
		{
			return _isTroopHero;
		}
		set
		{
			if (_isTroopHero != value)
			{
				_isTroopHero = value;
				OnPropertyChanged(value, "IsTroopHero");
				_isDirty = true;
			}
		}
	}

	public int CurrentAmount
	{
		get
		{
			return _currentAmount;
		}
		set
		{
			if (_currentAmount != value)
			{
				_currentAmount = value;
				OnPropertyChanged(value, "CurrentAmount");
				_isDirty = true;
			}
		}
	}

	public int MaxAmount
	{
		get
		{
			return _maxAmount;
		}
		set
		{
			if (_maxAmount != value)
			{
				_maxAmount = value;
				OnPropertyChanged(value, "MaxAmount");
				_isDirty = true;
			}
		}
	}

	public GameMenuTroopSelectionItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			AddButtonWidget.ClickEventHandlers.Add(OnAdd);
			RemoveButtonWidget.ClickEventHandlers.Add(OnRemove);
			_initialized = true;
		}
		if (_isDirty)
		{
			Refresh();
		}
	}

	private void OnRemove(Widget obj)
	{
		EventFired("Remove");
		Refresh();
	}

	private void OnAdd(Widget obj)
	{
		EventFired("Add");
		Refresh();
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (CurrentAmount == 0)
		{
			EventFired("Add");
		}
		else
		{
			EventFired("Remove");
		}
		Refresh();
	}

	private void Refresh()
	{
		if (CheckmarkVisualWidget == null || AddRemoveControls == null || AddButtonWidget == null || RemoveButtonWidget == null)
		{
			return;
		}
		if (MaxAmount == 0)
		{
			base.DoNotAcceptEvents = false;
			base.DoNotPassEventsToChildren = true;
			CheckmarkVisualWidget.IsHidden = CurrentAmount == 0;
			AddRemoveControls.IsHidden = true;
			AddButtonWidget.IsHidden = true;
			RemoveButtonWidget.IsHidden = true;
			base.IsDisabled = true;
			base.DominantSelectedState = IsLocked;
			HeroHealthParent.IsHidden = !IsTroopHero;
			if (IsLocked)
			{
				base.IsDisabled = CurrentAmount <= 0;
				base.DoNotPassEventsToChildren = true;
				base.DoNotAcceptEvents = true;
			}
		}
		else if (MaxAmount == 1)
		{
			base.DoNotAcceptEvents = false;
			base.DoNotPassEventsToChildren = true;
			CheckmarkVisualWidget.IsHidden = CurrentAmount == 0;
			AddRemoveControls.IsHidden = true;
			AddButtonWidget.IsHidden = true;
			RemoveButtonWidget.IsHidden = true;
			base.IsDisabled = (IsRosterFull && CurrentAmount <= 0) || IsLocked;
			base.DominantSelectedState = IsLocked;
			HeroHealthParent.IsHidden = !IsTroopHero;
			if (IsLocked)
			{
				base.IsDisabled = CurrentAmount <= 0;
				base.DoNotPassEventsToChildren = true;
				base.DoNotAcceptEvents = true;
			}
		}
		else
		{
			base.DoNotAcceptEvents = true;
			base.DoNotPassEventsToChildren = false;
			CheckmarkVisualWidget.IsHidden = true;
			AddRemoveControls.IsHidden = false;
			HeroHealthParent.IsHidden = true;
			AddButtonWidget.IsHidden = false;
			RemoveButtonWidget.IsHidden = false;
			AddButtonWidget.IsDisabled = IsRosterFull || CurrentAmount >= MaxAmount;
			RemoveButtonWidget.IsDisabled = CurrentAmount <= 0;
			if (IsLocked)
			{
				base.IsDisabled = false;
				base.DoNotPassEventsToChildren = true;
				base.DoNotAcceptEvents = true;
			}
		}
		base.GamepadNavigationIndex = (AddRemoveControls.IsVisible ? (-1) : 0);
	}
}
