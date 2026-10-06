using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleHeroItemVM : ViewModel
{
	private readonly TextObject _mismatchMountedText = new TextObject("{=J9V9YhkY}Captain is mounted!");

	private readonly TextObject _mismatchDismountedText = new TextObject("{=ufjypmaX}Captain is not mounted!");

	public static Action<OrderOfBattleHeroItemVM> OnHeroSelection;

	public static Action<OrderOfBattleHeroItemVM> OnHeroAssignedFormationChanged;

	public static Func<Agent, List<TooltipProperty>> GetAgentTooltip;

	public static Action<OrderOfBattleHeroItemVM> OnHeroAssignmentBegin;

	public static Action<OrderOfBattleHeroItemVM> OnHeroAssignmentEnd;

	public readonly Agent Agent;

	private List<TooltipProperty> _cachedTooltipProperties;

	private OrderOfBattleFormationItemVM _currentAssignedFormationItem;

	private string _mismatchedAssignmentDescriptionText;

	private bool _isAssignedToAFormation;

	private bool _isLeadingAFormation;

	private bool _hasMismatchedAssignment;

	private bool _isSelected;

	private bool _isDisabled;

	private bool _isShown;

	private bool _isMainHero;

	private CharacterImageIdentifierVM _imageIdentifier;

	private BasicTooltipViewModel _tooltip;

	private bool _isHighlightActive;

	public ItemObject BannerOfHero { get; private set; }

	public bool IsAssignedBeforePlayer { get; private set; }

	public Formation InitialFormation { get; private set; }

	public OrderOfBattleFormationItemVM InitialFormationItem { get; private set; }

	public OrderOfBattleFormationItemVM CurrentAssignedFormationItem
	{
		get
		{
			return _currentAssignedFormationItem;
		}
		set
		{
			if (value != _currentAssignedFormationItem)
			{
				_currentAssignedFormationItem = value;
				if (_currentAssignedFormationItem == null)
				{
					OnAssignmentRemoved();
				}
				IsAssignedToAFormation = _currentAssignedFormationItem != null;
				IsLeadingAFormation = _currentAssignedFormationItem != null && _currentAssignedFormationItem.Formation.Captain == Agent;
				OnAssignedFormationChanged();
			}
		}
	}

	[DataSourceProperty]
	public string MismatchedAssignmentDescriptionText
	{
		get
		{
			return _mismatchedAssignmentDescriptionText;
		}
		set
		{
			if (value != _mismatchedAssignmentDescriptionText)
			{
				_mismatchedAssignmentDescriptionText = value;
				OnPropertyChangedWithValue(value, "MismatchedAssignmentDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAssignedToAFormation
	{
		get
		{
			return _isAssignedToAFormation;
		}
		set
		{
			if (value != _isAssignedToAFormation)
			{
				_isAssignedToAFormation = value;
				OnPropertyChangedWithValue(value, "IsAssignedToAFormation");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLeadingAFormation
	{
		get
		{
			return _isLeadingAFormation;
		}
		set
		{
			if (value != _isLeadingAFormation)
			{
				_isLeadingAFormation = value;
				OnPropertyChangedWithValue(value, "IsLeadingAFormation");
			}
		}
	}

	[DataSourceProperty]
	public bool HasMismatchedAssignment
	{
		get
		{
			return _hasMismatchedAssignment;
		}
		set
		{
			if (value != _hasMismatchedAssignment)
			{
				_hasMismatchedAssignment = value;
				OnPropertyChangedWithValue(value, "HasMismatchedAssignment");
			}
		}
	}

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

	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	public bool IsShown
	{
		get
		{
			return _isShown;
		}
		set
		{
			if (value != _isShown)
			{
				_isShown = value;
				OnPropertyChangedWithValue(value, "IsShown");
			}
		}
	}

	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightActive
	{
		get
		{
			return _isHighlightActive;
		}
		set
		{
			if (value != _isHighlightActive)
			{
				_isHighlightActive = value;
				OnPropertyChangedWithValue(value, "IsHighlightActive");
			}
		}
	}

	public OrderOfBattleHeroItemVM()
	{
		IsDisabled = true;
		RefreshValues();
	}

	public OrderOfBattleHeroItemVM(Agent agent)
	{
		Agent = agent;
		BannerOfHero = agent.FormationBanner;
		IsDisabled = !Mission.Current.PlayerTeam.IsPlayerGeneral && !agent.IsMainAgent;
		IsShown = true;
		IsMainHero = Agent.IsMainAgent;
		ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(Agent.Character));
		Tooltip = new BasicTooltipViewModel(() => GetCaptainTooltip());
		RefreshValues();
	}

	public void SetInitialFormation(OrderOfBattleFormationItemVM formation)
	{
		if (InitialFormationItem != null)
		{
			Debug.FailedAssert("Initial formation for hero is already set", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleHeroItemVM.cs", "SetInitialFormation", 77);
		}
		if (formation != null)
		{
			InitialFormationItem = formation;
			InitialFormation = formation.Formation;
		}
	}

	public override void RefreshValues()
	{
		_cachedTooltipProperties = GetAgentTooltip?.Invoke(Agent);
	}

	private List<TooltipProperty> GetCaptainTooltip()
	{
		return _cachedTooltipProperties;
	}

	public void OnAssignmentRemoved()
	{
		if (CurrentAssignedFormationItem != null)
		{
			CurrentAssignedFormationItem.Formation.Refresh();
		}
		if (InitialFormation != null)
		{
			Agent.Formation = InitialFormation;
			InitialFormation.Refresh();
			if (Agent.IsDetachableFromFormation)
			{
				Agent.Team.DetachmentManager.RemoveScoresOfAgentFromDetachments(Agent);
			}
		}
	}

	public void RefreshInformation()
	{
		if (Agent != null)
		{
			ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(Agent.Character));
		}
		else
		{
			ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateEmpty());
		}
	}

	private void OnAssignedFormationChanged()
	{
		OnHeroAssignedFormationChanged?.Invoke(this);
		RefreshAssignmentInfo();
	}

	public void RefreshAssignmentInfo()
	{
		if (!IsLeadingAFormation)
		{
			HasMismatchedAssignment = false;
			return;
		}
		DeploymentFormationClass orderOfBattleClass = CurrentAssignedFormationItem.GetOrderOfBattleClass();
		if (Agent.HasMount)
		{
			if (orderOfBattleClass == DeploymentFormationClass.Infantry || orderOfBattleClass == DeploymentFormationClass.Ranged || orderOfBattleClass == DeploymentFormationClass.InfantryAndRanged)
			{
				HasMismatchedAssignment = true;
				MismatchedAssignmentDescriptionText = _mismatchMountedText.ToString();
			}
		}
		else if (orderOfBattleClass == DeploymentFormationClass.Cavalry || orderOfBattleClass == DeploymentFormationClass.HorseArcher || orderOfBattleClass == DeploymentFormationClass.CavalryAndHorseArcher)
		{
			HasMismatchedAssignment = true;
			MismatchedAssignmentDescriptionText = _mismatchDismountedText.ToString();
		}
	}

	public void SetIsPreAssigned(bool isPreAssigned)
	{
		IsAssignedBeforePlayer = isPreAssigned;
	}

	private void ExecuteSelection()
	{
		OnHeroSelection?.Invoke(this);
	}

	private void ExecuteBeginAssignment()
	{
		OnHeroAssignmentBegin?.Invoke(this);
	}

	private void ExecuteEndAssignment()
	{
		OnHeroAssignmentEnd?.Invoke(this);
	}
}
