using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class SettlementBuildingProjectVM : SettlementProjectVM
{
	private bool _isSelected;

	private string _alreadyAtMaxText;

	private string _developmentLevelText;

	private int _level;

	private int _maxLevel;

	private int _developmentQueueIndex = -1;

	private bool _canBuild;

	private bool _isInQueue;

	private HintViewModel _addRemoveHint;

	private HintViewModel _setAsActiveHint;

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public string DevelopmentLevelText
	{
		get
		{
			return _developmentLevelText;
		}
		set
		{
			if (value != _developmentLevelText)
			{
				_developmentLevelText = value;
				OnPropertyChangedWithValue(value, "DevelopmentLevelText");
			}
		}
	}

	[DataSourceProperty]
	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "Level");
			}
		}
	}

	[DataSourceProperty]
	public int MaxLevel
	{
		get
		{
			return _maxLevel;
		}
		set
		{
			if (value != _maxLevel)
			{
				_maxLevel = value;
				OnPropertyChangedWithValue(value, "MaxLevel");
			}
		}
	}

	[DataSourceProperty]
	public int DevelopmentQueueIndex
	{
		get
		{
			return _developmentQueueIndex;
		}
		set
		{
			if (value != _developmentQueueIndex)
			{
				_developmentQueueIndex = value;
				OnPropertyChangedWithValue(value, "DevelopmentQueueIndex");
				UpdateProjectHints();
			}
		}
	}

	[DataSourceProperty]
	public bool IsInQueue
	{
		get
		{
			return _isInQueue;
		}
		set
		{
			if (value != _isInQueue)
			{
				_isInQueue = value;
				OnPropertyChangedWithValue(value, "IsInQueue");
				UpdateProjectHints();
			}
		}
	}

	[DataSourceProperty]
	public string AlreadyAtMaxText
	{
		get
		{
			return _alreadyAtMaxText;
		}
		set
		{
			if (value != _alreadyAtMaxText)
			{
				_alreadyAtMaxText = value;
				OnPropertyChangedWithValue(value, "AlreadyAtMaxText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBuild
	{
		get
		{
			return _canBuild;
		}
		set
		{
			if (value != _canBuild)
			{
				_canBuild = value;
				OnPropertyChangedWithValue(value, "CanBuild");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AddRemoveHint
	{
		get
		{
			return _addRemoveHint;
		}
		set
		{
			if (value != _addRemoveHint)
			{
				_addRemoveHint = value;
				OnPropertyChangedWithValue(value, "AddRemoveHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SetAsActiveHint
	{
		get
		{
			return _setAsActiveHint;
		}
		set
		{
			if (value != _setAsActiveHint)
			{
				_setAsActiveHint = value;
				OnPropertyChangedWithValue(value, "SetAsActiveHint");
			}
		}
	}

	public SettlementBuildingProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
		: base(onSelection, onSetAsCurrent, onResetCurrent, building, settlement)
	{
		Level = building.CurrentLevel;
		MaxLevel = 3;
		DevelopmentLevelText = building.CurrentLevel.ToString();
		CanBuild = Level < 3;
		base.IsDaily = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AlreadyAtMaxText = new TextObject("{=ybLA7ZXp}Already at Max").ToString();
		UpdateProjectHints();
	}

	private void UpdateProjectHints()
	{
		if (AddRemoveHint == null)
		{
			AddRemoveHint = new HintViewModel();
		}
		if (SetAsActiveHint == null)
		{
			SetAsActiveHint = new HintViewModel();
		}
		AddRemoveHint.HintText = (IsInQueue ? new TextObject("{=faDegful}Remove from queue") : new TextObject("{=SFebv4hH}Add to queue"));
		SetAsActiveHint.HintText = ((DevelopmentQueueIndex == 0) ? new TextObject("{=cD1HTdYJ}Already active development") : new TextObject("{=PcLGc2bM}Set as active development"));
	}

	public override void RefreshProductionText()
	{
		base.RefreshProductionText();
		if (DevelopmentQueueIndex == 0)
		{
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_completion"));
			int daysToComplete = BuildingHelper.GetDaysToComplete(base.Building, _settlement.Town);
			TextObject textObject;
			if (daysToComplete != -1)
			{
				textObject = new TextObject("{=c5eYzHaM}{DAYS} {?DAY_IS_PLURAL}Days{?}Day{\\?} ({PERCENTAGE}%)");
				textObject.SetTextVariable("DAYS", daysToComplete);
				GameTexts.SetVariable("DAY_IS_PLURAL", (daysToComplete > 1) ? 1 : 0);
			}
			else
			{
				textObject = new TextObject("{=0TauthlH}Never ({PERCENTAGE}%)");
			}
			textObject.SetTextVariable("PERCENTAGE", (int)(BuildingHelper.GetProgressOfBuilding(base.Building, _settlement.Town) * 100f));
			GameTexts.SetVariable("RIGHT", textObject);
			base.ProductionText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
		}
		else if (DevelopmentQueueIndex > 0)
		{
			GameTexts.SetVariable("NUMBER", DevelopmentQueueIndex);
			base.ProductionText = GameTexts.FindText("str_in_queue_with_number").ToString();
		}
		else
		{
			base.ProductionText = " ";
		}
	}

	public override void ExecuteAddRemoveToQueue()
	{
		if (_onSelection != null && CanBuild)
		{
			_onSelection(this, arg2: false);
		}
	}

	public override void ExecuteSetAsActiveDevelopment()
	{
		if (_onSelection != null && CanBuild)
		{
			_onSelection(this, arg2: true);
		}
	}

	public override void ExecuteSetAsCurrent()
	{
		_onSetAsCurrent?.Invoke(this);
	}

	public override void ExecuteResetCurrent()
	{
		_onResetCurrent?.Invoke();
	}

	public override void ExecuteToggleSelected()
	{
		if (CanBuild)
		{
			IsSelected = !IsSelected;
		}
	}
}
