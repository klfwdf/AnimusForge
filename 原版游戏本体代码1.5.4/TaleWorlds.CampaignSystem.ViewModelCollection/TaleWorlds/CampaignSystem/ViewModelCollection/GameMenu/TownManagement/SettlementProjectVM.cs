using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public abstract class SettlementProjectVM : ViewModel
{
	public int Index;

	private Building _building;

	protected Action<SettlementProjectVM, bool> _onSelection;

	protected Action<SettlementProjectVM> _onSetAsCurrent;

	protected Action _onResetCurrent;

	protected Settlement _settlement;

	private readonly TextObject L1BonusText = new TextObject("{=PJZ8QYgA}L-I : {BONUS}");

	private readonly TextObject L2BonusText = new TextObject("{=9i0wnjJK}L-II : {BONUS}");

	private readonly TextObject L3BonusText = new TextObject("{=pRP2sOWP}L-III : {BONUS}");

	private string _name;

	private string _visualCode;

	private string _explanation;

	private string _currentPositiveEffectText;

	private string _nextPositiveEffectText;

	private string _productionCostText;

	private int _progress;

	private bool _isCurrentActiveProject;

	private string _productionText;

	public bool IsDaily { get; protected set; }

	public Building Building
	{
		get
		{
			return _building;
		}
		set
		{
			_building = value;
			Name = ((value != null) ? value.Name.ToString() : "");
			Explanation = ((value != null) ? value.Explanation.ToString() : "");
			VisualCode = ((value != null) ? value.BuildingType.StringId.ToLower() : "");
			int constructionCost = Building.GetConstructionCost();
			TextObject textObject;
			if (constructionCost > 0)
			{
				textObject = new TextObject("{=tAwRIPiy}Construction Cost: {COST}");
				textObject.SetTextVariable("COST", constructionCost);
			}
			else
			{
				textObject = TextObject.GetEmpty();
			}
			ProductionCostText = ((value != null) ? textObject.ToString() : "");
			CurrentPositiveEffectText = ((value != null) ? value.GetBonusExplanation().ToString() : "");
		}
	}

	[DataSourceProperty]
	public string VisualCode
	{
		get
		{
			return _visualCode;
		}
		set
		{
			if (value != _visualCode)
			{
				_visualCode = value;
				OnPropertyChangedWithValue(value, "VisualCode");
			}
		}
	}

	[DataSourceProperty]
	public string ProductionText
	{
		get
		{
			return _productionText;
		}
		set
		{
			if (value != _productionText)
			{
				_productionText = value;
				OnPropertyChangedWithValue(value, "ProductionText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentPositiveEffectText
	{
		get
		{
			return _currentPositiveEffectText;
		}
		set
		{
			if (value != _currentPositiveEffectText)
			{
				_currentPositiveEffectText = value;
				OnPropertyChangedWithValue(value, "CurrentPositiveEffectText");
			}
		}
	}

	[DataSourceProperty]
	public string NextPositiveEffectText
	{
		get
		{
			return _nextPositiveEffectText;
		}
		set
		{
			if (value != _nextPositiveEffectText)
			{
				_nextPositiveEffectText = value;
				OnPropertyChangedWithValue(value, "NextPositiveEffectText");
			}
		}
	}

	[DataSourceProperty]
	public string ProductionCostText
	{
		get
		{
			return _productionCostText;
		}
		set
		{
			if (value != _productionCostText)
			{
				_productionCostText = value;
				OnPropertyChangedWithValue(value, "ProductionCostText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentActiveProject
	{
		get
		{
			return _isCurrentActiveProject;
		}
		set
		{
			if (value != _isCurrentActiveProject)
			{
				_isCurrentActiveProject = value;
				OnPropertyChangedWithValue(value, "IsCurrentActiveProject");
			}
		}
	}

	[DataSourceProperty]
	public int Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (value != _progress)
			{
				_progress = value;
				OnPropertyChangedWithValue(value, "Progress");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string Explanation
	{
		get
		{
			return _explanation;
		}
		set
		{
			if (value != _explanation)
			{
				_explanation = value;
				OnPropertyChangedWithValue(value, "Explanation");
			}
		}
	}

	protected SettlementProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
	{
		_onSelection = onSelection;
		_onSetAsCurrent = onSetAsCurrent;
		_onResetCurrent = onResetCurrent;
		Building = building;
		_settlement = settlement;
		Progress = (int)(BuildingHelper.GetProgressOfBuilding(building, _settlement.Town) * 100f);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Building.BuildingType.IsDailyProject)
		{
			CurrentPositiveEffectText = Building.BuildingType.GetExplanationAtLevel(Building.CurrentLevel).ToString();
			NextPositiveEffectText = "";
		}
		else
		{
			CurrentPositiveEffectText = GetBonusText(Building, Building.CurrentLevel);
			NextPositiveEffectText = GetBonusText(Building, Building.CurrentLevel + 1);
		}
	}

	private string GetBonusText(Building building, int level)
	{
		TextObject textObject;
		switch (level)
		{
		case 0:
		case 4:
			return "";
		default:
			textObject = L3BonusText;
			break;
		case 2:
			textObject = L2BonusText;
			break;
		case 1:
			textObject = L1BonusText;
			break;
		}
		TextObject bonusExplanationOfLevel = GetBonusExplanationOfLevel(level);
		textObject.SetTextVariable("BONUS", bonusExplanationOfLevel);
		return textObject.ToString();
	}

	private void ExecuteShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(Building), _building);
	}

	private void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}

	private TextObject GetBonusExplanationOfLevel(int level)
	{
		if (level >= 0 && level <= 3)
		{
			return Building.BuildingType.GetExplanationAtLevel(level);
		}
		return TextObject.GetEmpty();
	}

	public virtual void RefreshProductionText()
	{
	}

	public abstract void ExecuteAddRemoveToQueue();

	public abstract void ExecuteSetAsActiveDevelopment();

	public abstract void ExecuteSetAsCurrent();

	public abstract void ExecuteResetCurrent();

	public abstract void ExecuteToggleSelected();
}
