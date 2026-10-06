using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.CampaignBehaviors;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace NavalDLC.ViewModelCollection.OrderOfBattle;

public class NavalOrderOfBattleVM : ViewModel
{
	private readonly MBList<NavalOrderOfBattleFormationItemVM> _allFormations;

	private readonly List<NavalOrderOfBattleHeroItemVM> _allHeroes;

	private readonly List<NavalOrderOfBattleShipItemVM> _allShips;

	private NavalShipsLogic _navalShipsLogic;

	private NavalDeploymentMissionController _navalDeploymentController;

	private OrderController _orderController;

	private NavalOrderOfBattleCampaignBehavior _navalOrderOfBattleCampaignBehavior;

	private AssignPlayerRoleInTeamMissionController _assignPlayerRoleInTeamMissioncontroller;

	private readonly Action<NavalOrderOfBattleFormationItemVM> _onFormationSelected;

	private readonly Action _clearFormationSelection;

	private readonly Action _onAutoDeploy;

	private readonly Action _onBeginMission;

	private readonly Mission _mission;

	private readonly TextObject _formationsDisabledHintGeneral = new TextObject("{=ZixS1b4u}You're not leading this battle.");

	private readonly TextObject _formationsDisabledHintAllies = new TextObject("{=O4n4SAqo}Formation is reserved for allied parties.");

	private readonly TextObject _formationsDisabledHintSkills = new TextObject("{=Vs5NavCd}You do not have enough skills/perks for this formation.");

	private readonly TextObject _formationsDisabledHintShips = new TextObject("{=bID6axoH}You do not have enough ships for this formation.");

	private bool _finalizeInitializationOnNextUpdate;

	private bool _isLoadingConfigurationAgents;

	private bool _isEnabled;

	private bool _isAssignmentDirty;

	private bool _canStartMission;

	private bool _isPlayerGeneral;

	private bool _areCameraControlsEnabled;

	private bool _hasSelectedHero;

	private bool _hasSelectedShip;

	private string _beginMissionText;

	private string _autoDeployText;

	private NavalOrderOfBattleShipItemVM _selectedShip;

	private NavalOrderOfBattleHeroItemVM _selectedHero;

	private MBBindingList<NavalOrderOfBattleFormationItemVM> _leftFormations;

	private MBBindingList<NavalOrderOfBattleFormationItemVM> _rightFormations;

	private MBBindingList<NavalOrderOfBattleHeroItemVM> _unassignedHeroes;

	private MBBindingList<NavalOrderOfBattleShipItemVM> _unassignedShips;

	private bool _areHotkeysEnabled;

	private bool _isPoolAcceptingHero;

	private bool _isPoolAcceptingShip;

	private HintViewModel _canStartHint;

	private bool _canToggleHeroOrShipSelection;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _resetInputKey;

	public MBReadOnlyList<NavalOrderOfBattleFormationItemVM> AllFormations => _allFormations;

	public List<MissionOrderVM.FormationConfiguration> CurrentFilterConfiguration { get; private set; }

	public List<MissionOrderVM.ClassConfiguration> CurrentClassConfiguration { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAssignmentDirty
	{
		get
		{
			return _isAssignmentDirty;
		}
		set
		{
			if (value != _isAssignmentDirty)
			{
				_isAssignmentDirty = value;
				OnPropertyChangedWithValue(value, "IsAssignmentDirty");
			}
		}
	}

	[DataSourceProperty]
	public bool CanStartMission
	{
		get
		{
			return _canStartMission;
		}
		set
		{
			if (value != _canStartMission)
			{
				_canStartMission = value;
				OnPropertyChangedWithValue(value, "CanStartMission");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerGeneral
	{
		get
		{
			return _isPlayerGeneral;
		}
		set
		{
			if (value != _isPlayerGeneral)
			{
				_isPlayerGeneral = value;
				OnPropertyChangedWithValue(value, "IsPlayerGeneral");
			}
		}
	}

	[DataSourceProperty]
	public bool AreCameraControlsEnabled
	{
		get
		{
			return _areCameraControlsEnabled;
		}
		set
		{
			if (value != _areCameraControlsEnabled)
			{
				_areCameraControlsEnabled = value;
				OnPropertyChangedWithValue(value, "AreCameraControlsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSelectedHero
	{
		get
		{
			return _hasSelectedHero;
		}
		set
		{
			if (value != _hasSelectedHero)
			{
				_hasSelectedHero = value;
				OnPropertyChangedWithValue(value, "HasSelectedHero");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSelectedShip
	{
		get
		{
			return _hasSelectedShip;
		}
		set
		{
			if (value != _hasSelectedShip)
			{
				_hasSelectedShip = value;
				OnPropertyChangedWithValue(value, "HasSelectedShip");
			}
		}
	}

	[DataSourceProperty]
	public string BeginMissionText
	{
		get
		{
			return _beginMissionText;
		}
		set
		{
			if (value != _beginMissionText)
			{
				_beginMissionText = value;
				OnPropertyChangedWithValue(value, "BeginMissionText");
			}
		}
	}

	[DataSourceProperty]
	public string AutoDeployText
	{
		get
		{
			return _autoDeployText;
		}
		set
		{
			if (value != _autoDeployText)
			{
				_autoDeployText = value;
				OnPropertyChangedWithValue(value, "AutoDeployText");
			}
		}
	}

	[DataSourceProperty]
	public NavalOrderOfBattleShipItemVM SelectedShip
	{
		get
		{
			return _selectedShip;
		}
		set
		{
			if (value != _selectedShip)
			{
				if (_selectedShip != null)
				{
					_selectedShip.IsSelected = false;
				}
				_selectedShip = value;
				OnPropertyChangedWithValue(value, "SelectedShip");
				HasSelectedShip = _selectedShip != null;
				if (_selectedShip != null)
				{
					_selectedShip.IsSelected = true;
				}
				OnSelectionUpdated();
			}
		}
	}

	[DataSourceProperty]
	public NavalOrderOfBattleHeroItemVM SelectedHero
	{
		get
		{
			return _selectedHero;
		}
		set
		{
			if (value != _selectedHero)
			{
				if (_selectedHero != null)
				{
					_selectedHero.IsSelected = false;
				}
				_selectedHero = value;
				OnPropertyChangedWithValue(value, "SelectedHero");
				HasSelectedHero = _selectedHero != null;
				if (_selectedHero != null)
				{
					_selectedHero.IsSelected = true;
				}
				OnSelectionUpdated();
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalOrderOfBattleFormationItemVM> LeftFormations
	{
		get
		{
			return _leftFormations;
		}
		set
		{
			if (value != _leftFormations)
			{
				_leftFormations = value;
				OnPropertyChangedWithValue(value, "LeftFormations");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalOrderOfBattleFormationItemVM> RightFormations
	{
		get
		{
			return _rightFormations;
		}
		set
		{
			if (value != _rightFormations)
			{
				_rightFormations = value;
				OnPropertyChangedWithValue(value, "RightFormations");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalOrderOfBattleHeroItemVM> UnassignedHeroes
	{
		get
		{
			return _unassignedHeroes;
		}
		set
		{
			if (value != _unassignedHeroes)
			{
				_unassignedHeroes = value;
				OnPropertyChangedWithValue(value, "UnassignedHeroes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalOrderOfBattleShipItemVM> UnassignedShips
	{
		get
		{
			return _unassignedShips;
		}
		set
		{
			if (value != _unassignedShips)
			{
				_unassignedShips = value;
				OnPropertyChangedWithValue(value, "UnassignedShips");
			}
		}
	}

	[DataSourceProperty]
	public bool AreHotkeysEnabled
	{
		get
		{
			return _areHotkeysEnabled;
		}
		set
		{
			if (value != _areHotkeysEnabled)
			{
				_areHotkeysEnabled = value;
				OnPropertyChangedWithValue(value, "AreHotkeysEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPoolAcceptingHero
	{
		get
		{
			return _isPoolAcceptingHero;
		}
		set
		{
			if (value != _isPoolAcceptingHero)
			{
				_isPoolAcceptingHero = value;
				OnPropertyChangedWithValue(value, "IsPoolAcceptingHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPoolAcceptingShip
	{
		get
		{
			return _isPoolAcceptingShip;
		}
		set
		{
			if (value != _isPoolAcceptingShip)
			{
				_isPoolAcceptingShip = value;
				OnPropertyChangedWithValue(value, "IsPoolAcceptingShip");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CanStartHint
	{
		get
		{
			return _canStartHint;
		}
		set
		{
			if (value != _canStartHint)
			{
				_canStartHint = value;
				OnPropertyChangedWithValue(value, "CanStartHint");
			}
		}
	}

	[DataSourceProperty]
	public bool CanToggleHeroOrShipSelection
	{
		get
		{
			return _canToggleHeroOrShipSelection;
		}
		set
		{
			if (value != _canToggleHeroOrShipSelection)
			{
				_canToggleHeroOrShipSelection = value;
				OnPropertyChangedWithValue(value, "CanToggleHeroOrShipSelection");
			}
		}
	}

	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	public NavalOrderOfBattleVM(Mission mission, Action<NavalOrderOfBattleFormationItemVM> onFormationSelected, Action clearFormationSelection, Action onAutoDeploy, Action onBeginMission)
	{
		_mission = mission;
		_onFormationSelected = onFormationSelected;
		_clearFormationSelection = clearFormationSelection;
		_onAutoDeploy = onAutoDeploy;
		_onBeginMission = onBeginMission;
		_allFormations = new MBList<NavalOrderOfBattleFormationItemVM>();
		LeftFormations = new MBBindingList<NavalOrderOfBattleFormationItemVM>();
		RightFormations = new MBBindingList<NavalOrderOfBattleFormationItemVM>();
		_allHeroes = new List<NavalOrderOfBattleHeroItemVM>();
		_allShips = new List<NavalOrderOfBattleShipItemVM>();
		UnassignedHeroes = new MBBindingList<NavalOrderOfBattleHeroItemVM>();
		UnassignedShips = new MBBindingList<NavalOrderOfBattleShipItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BeginMissionText = new TextObject("{=SYYOSOoa}Ready").ToString();
		AutoDeployText = GameTexts.FindText("str_auto_deploy").ToString();
		_allHeroes.ForEach(delegate(NavalOrderOfBattleHeroItemVM h)
		{
			h.RefreshValues();
		});
		_allShips.ForEach(delegate(NavalOrderOfBattleShipItemVM s)
		{
			s.RefreshValues();
		});
		LeftFormations.ApplyActionOnAllItems(delegate(NavalOrderOfBattleFormationItemVM f)
		{
			f.RefreshValues();
		});
		RightFormations.ApplyActionOnAllItems(delegate(NavalOrderOfBattleFormationItemVM f)
		{
			f.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (IsEnabled)
		{
			SaveConfiguration();
		}
		if (_navalDeploymentController != null)
		{
			_navalDeploymentController.PlayerShipsUpdated -= OnPlayerShipsUpdated;
			_navalDeploymentController = null;
		}
		if (_orderController != null)
		{
			_orderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
			_orderController = null;
		}
		NavalOrderOfBattleFormationItemVM.OnAcceptCaptain = (Action<NavalOrderOfBattleFormationItemVM>)Delegate.Remove(NavalOrderOfBattleFormationItemVM.OnAcceptCaptain, new Action<NavalOrderOfBattleFormationItemVM>(OnFormationAcceptCaptain));
		NavalOrderOfBattleFormationItemVM.OnAcceptShip = (Action<NavalOrderOfBattleFormationItemVM>)Delegate.Remove(NavalOrderOfBattleFormationItemVM.OnAcceptShip, new Action<NavalOrderOfBattleFormationItemVM>(OnFormationAcceptShip));
		NavalOrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter = (Func<DeploymentFormationClass, FormationFilterType, int>)Delegate.Remove(NavalOrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter, new Func<DeploymentFormationClass, FormationFilterType, int>(GetTroopCountWithFilter));
		IsEnabled = false;
		DoneInputKey?.OnFinalize();
		DoneInputKey = null;
		ResetInputKey?.OnFinalize();
		ResetInputKey = null;
		LeftFormations.ApplyActionOnAllItems(delegate(NavalOrderOfBattleFormationItemVM f)
		{
			f.OnFinalize();
		});
		LeftFormations.Clear();
		RightFormations.ApplyActionOnAllItems(delegate(NavalOrderOfBattleFormationItemVM f)
		{
			f.OnFinalize();
		});
		RightFormations.Clear();
		_allFormations.Clear();
		_allHeroes.ForEach(delegate(NavalOrderOfBattleHeroItemVM h)
		{
			h.OnFinalize();
		});
		_allShips.ForEach(delegate(NavalOrderOfBattleShipItemVM s)
		{
			s.OnFinalize();
		});
		_allHeroes.Clear();
		_allShips.Clear();
		UnassignedHeroes.Clear();
		UnassignedShips.Clear();
	}

	public void Initialize()
	{
		_navalShipsLogic = _mission.GetMissionBehavior<NavalShipsLogic>();
		_navalDeploymentController = _mission.GetMissionBehavior<NavalDeploymentMissionController>();
		_assignPlayerRoleInTeamMissioncontroller = _mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>();
		_navalDeploymentController.PlayerShipsUpdated += OnPlayerShipsUpdated;
		_orderController = _mission.PlayerTeam.PlayerOrderController;
		_orderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
		NavalOrderOfBattleFormationItemVM.OnAcceptCaptain = (Action<NavalOrderOfBattleFormationItemVM>)Delegate.Combine(NavalOrderOfBattleFormationItemVM.OnAcceptCaptain, new Action<NavalOrderOfBattleFormationItemVM>(OnFormationAcceptCaptain));
		NavalOrderOfBattleFormationItemVM.OnAcceptShip = (Action<NavalOrderOfBattleFormationItemVM>)Delegate.Combine(NavalOrderOfBattleFormationItemVM.OnAcceptShip, new Action<NavalOrderOfBattleFormationItemVM>(OnFormationAcceptShip));
		NavalOrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter = (Func<DeploymentFormationClass, FormationFilterType, int>)Delegate.Combine(NavalOrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter, new Func<DeploymentFormationClass, FormationFilterType, int>(GetTroopCountWithFilter));
		IsPlayerGeneral = _mission.PlayerTeam.IsPlayerGeneral;
		CurrentFilterConfiguration = new List<MissionOrderVM.FormationConfiguration>();
		CurrentClassConfiguration = new List<MissionOrderVM.ClassConfiguration>();
		RefreshAll();
		_navalOrderOfBattleCampaignBehavior = Campaign.Current?.GetCampaignBehavior<NavalOrderOfBattleCampaignBehavior>();
		LoadConfigurationShips();
		if (IsAssignmentDirty)
		{
			_finalizeInitializationOnNextUpdate = true;
		}
		else
		{
			FinalizeInitialization();
		}
		IsEnabled = true;
	}

	public void ExecuteAutoDeploy()
	{
		if (!IsAssignmentDirty)
		{
			IsAssignmentDirty = true;
			_onAutoDeploy?.Invoke();
		}
	}

	public void ExecuteBeginMission()
	{
		if (IsAssignmentDirty || !CanStartMission)
		{
			return;
		}
		CurrentFilterConfiguration?.Clear();
		CurrentClassConfiguration?.Clear();
		foreach (NavalOrderOfBattleFormationItemVM allFormation in AllFormations)
		{
			if (allFormation.Formation.CountOfUnits > 0)
			{
				CurrentFilterConfiguration?.Add(new MissionOrderVM.FormationConfiguration(allFormation.Formation.Index, (from f in allFormation.FilterItems
					where f.IsActive
					select f.FilterType).ToList()));
				CurrentClassConfiguration?.Add(new MissionOrderVM.ClassConfiguration(allFormation.Formation.Index, allFormation.SelectedClass));
			}
			else
			{
				CurrentFilterConfiguration?.Add(new MissionOrderVM.FormationConfiguration(allFormation.Formation.Index, new List<FormationFilterType>()));
				CurrentClassConfiguration?.Add(new MissionOrderVM.ClassConfiguration(allFormation.Formation.Index, DeploymentFormationClass.Infantry));
			}
		}
		_onBeginMission?.Invoke();
		MBInformationManager.HideInformations();
	}

	public void ExecuteClearHeroAndShipSelection()
	{
		SelectedHero = null;
		SelectedShip = null;
	}

	public bool OnEscape()
	{
		bool result = false;
		if (AllFormations.Any((NavalOrderOfBattleFormationItemVM x) => x.IsSelected))
		{
			_clearFormationSelection?.Invoke();
			result = true;
		}
		return result;
	}

	private void RefreshFormations()
	{
		if (AllFormations.Count != 0)
		{
			return;
		}
		MBReadOnlyList<Formation> usableFormations = _navalDeploymentController.GetUsableFormations();
		for (int i = 0; i < usableFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM item = new NavalOrderOfBattleFormationItemVM(usableFormations[i], OnFormationSelected, OnClassChanged, OnFilterUseToggled);
			if (i < usableFormations.Count / 2)
			{
				LeftFormations.Add(item);
			}
			else
			{
				RightFormations.Add(item);
			}
			_allFormations.Add(item);
		}
	}

	private void RefreshShips()
	{
		if (_allShips.Count == 0)
		{
			foreach (IShipOrigin allPlayerShip in _navalDeploymentController.GetAllPlayerShips())
			{
				_allShips.Add(new NavalOrderOfBattleShipItemVM(allPlayerShip, OnShipSelected, FindFormationOfShip));
			}
		}
		for (int i = 0; i < _allShips.Count; i++)
		{
			NavalOrderOfBattleShipItemVM navalOrderOfBattleShipItemVM = _allShips[i];
			ShipAssignment shipAssignment;
			bool flag = _navalShipsLogic.FindAssignmentOfShipOrigin(navalOrderOfBattleShipItemVM.ShipOrigin, out shipAssignment);
			int isDisabled;
			if (!IsPlayerGeneral)
			{
				if (PartyBase.MainParty.Ships.Contains(navalOrderOfBattleShipItemVM.ShipOrigin))
				{
					if (flag)
					{
						Agent captain = shipAssignment.Formation.Captain;
						isDisabled = ((captain == null || !captain.IsMainAgent) ? 1 : 0);
					}
					else
					{
						isDisabled = 0;
					}
				}
				else
				{
					isDisabled = 1;
				}
			}
			else
			{
				isDisabled = 0;
			}
			navalOrderOfBattleShipItemVM.IsDisabled = (byte)isDisabled != 0;
			if (flag)
			{
				navalOrderOfBattleShipItemVM.MissionShip = shipAssignment.MissionShip;
				if (UnassignedShips.Contains(navalOrderOfBattleShipItemVM))
				{
					UnassignedShips.Remove(navalOrderOfBattleShipItemVM);
				}
				for (int j = 0; j < AllFormations.Count; j++)
				{
					if (AllFormations[j].Formation == shipAssignment.Formation && AllFormations[j].Ship != navalOrderOfBattleShipItemVM)
					{
						AllFormations[j].Ship = navalOrderOfBattleShipItemVM;
					}
					else if (AllFormations[j].Formation != shipAssignment.Formation && AllFormations[j].Ship == navalOrderOfBattleShipItemVM)
					{
						AllFormations[j].Ship = null;
					}
				}
				continue;
			}
			navalOrderOfBattleShipItemVM.MissionShip = null;
			for (int k = 0; k < AllFormations.Count; k++)
			{
				if (AllFormations[k].Ship == navalOrderOfBattleShipItemVM)
				{
					AllFormations[k].Ship = null;
				}
			}
			if (!navalOrderOfBattleShipItemVM.IsDisabled && !UnassignedShips.Contains(navalOrderOfBattleShipItemVM))
			{
				UnassignedShips.Add(navalOrderOfBattleShipItemVM);
			}
			else if (navalOrderOfBattleShipItemVM.IsDisabled && UnassignedShips.Contains(navalOrderOfBattleShipItemVM))
			{
				UnassignedShips.Remove(navalOrderOfBattleShipItemVM);
			}
		}
	}

	private void RefreshHeroes()
	{
		if (_allHeroes.Count == 0)
		{
			foreach (IAgentOriginBase allPlayerTeamHero in _navalDeploymentController.GetAllPlayerTeamHeroes())
			{
				_allHeroes.Add(new NavalOrderOfBattleHeroItemVM(allPlayerTeamHero, OnHeroSelected));
			}
		}
		for (int i = 0; i < _allHeroes.Count; i++)
		{
			NavalOrderOfBattleHeroItemVM heroVM = _allHeroes[i];
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations.FirstOrDefault((NavalOrderOfBattleFormationItemVM x) => x.Formation.Captain?.Origin == heroVM.AgentOrigin);
			heroVM.IsDisabled = !IsPlayerGeneral && !heroVM.IsMainHero;
			if (navalOrderOfBattleFormationItemVM != null)
			{
				if (UnassignedHeroes.Contains(heroVM))
				{
					UnassignedHeroes.Remove(heroVM);
				}
				for (int num = 0; num < AllFormations.Count; num++)
				{
					NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM2 = AllFormations[num];
					if (navalOrderOfBattleFormationItemVM2 == navalOrderOfBattleFormationItemVM && navalOrderOfBattleFormationItemVM2.Captain != heroVM)
					{
						navalOrderOfBattleFormationItemVM2.Captain = heroVM;
					}
					else if (navalOrderOfBattleFormationItemVM2 != navalOrderOfBattleFormationItemVM && navalOrderOfBattleFormationItemVM2.Captain == heroVM)
					{
						navalOrderOfBattleFormationItemVM2.Captain = null;
					}
				}
				continue;
			}
			for (int num2 = 0; num2 < AllFormations.Count; num2++)
			{
				if (AllFormations[num2].Captain == heroVM)
				{
					AllFormations[num2].Captain = null;
				}
			}
			if (!heroVM.IsDisabled && !UnassignedHeroes.Contains(heroVM))
			{
				UnassignedHeroes.Add(heroVM);
			}
			else if (heroVM.IsDisabled && UnassignedHeroes.Contains(heroVM))
			{
				UnassignedHeroes.Remove(heroVM);
			}
		}
	}

	private void RefreshFormationsDisabledAndReason()
	{
		_navalShipsLogic.GetShipDeploymentLimit(TeamSideEnum.PlayerTeam, out var deploymentLimit);
		NavalShipDeploymentLimit deploymentLimit2;
		int shipDeploymentLimit = _navalShipsLogic.GetShipDeploymentLimit(TeamSideEnum.PlayerAllyTeam, out deploymentLimit2);
		int num = _allShips.Count((NavalOrderOfBattleShipItemVM x) => !x.IsDisabled);
		for (int num2 = 0; num2 < AllFormations.Count; num2++)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations[num2];
			int num3 = num2 + 1;
			if (navalOrderOfBattleFormationItemVM.Formation.PlayerOwner != _mission.InitialPlayerAgent)
			{
				navalOrderOfBattleFormationItemVM.IsEnabled = false;
				navalOrderOfBattleFormationItemVM.DisabledHint = new HintViewModel(_formationsDisabledHintGeneral);
			}
			else if (num3 > 8 - shipDeploymentLimit)
			{
				navalOrderOfBattleFormationItemVM.IsEnabled = false;
				navalOrderOfBattleFormationItemVM.DisabledHint = new HintViewModel(_formationsDisabledHintAllies);
			}
			else if (num3 > deploymentLimit.PartiesLimit)
			{
				navalOrderOfBattleFormationItemVM.IsEnabled = false;
				navalOrderOfBattleFormationItemVM.DisabledHint = new HintViewModel(_formationsDisabledHintSkills);
			}
			else if (num3 > num)
			{
				navalOrderOfBattleFormationItemVM.IsEnabled = false;
				navalOrderOfBattleFormationItemVM.DisabledHint = new HintViewModel(_formationsDisabledHintShips);
			}
			else
			{
				navalOrderOfBattleFormationItemVM.IsEnabled = true;
				navalOrderOfBattleFormationItemVM.DisabledHint = null;
			}
		}
	}

	private void RefreshCanStartMission()
	{
		if (!IsPlayerGeneral)
		{
			CanStartMission = true;
			CanStartHint = null;
		}
		if (AllFormations.Any((NavalOrderOfBattleFormationItemVM x) => x.IsMinimumCrewCountWarningActive) && AllFormations.Count((NavalOrderOfBattleFormationItemVM x) => x.HasShip) > 1)
		{
			CanStartMission = false;
			CanStartHint = new HintViewModel((AllFormations.Count((NavalOrderOfBattleFormationItemVM x) => x.IsMinimumCrewCountWarningActive) > 1) ? new TextObject("{=cabaUQjc}There are ships with too few troops!") : new TextObject("{=XjtHpi22}There is a ship with too few troops!"));
		}
		else
		{
			CanStartMission = true;
			CanStartHint = null;
		}
	}

	private void FinalizeInitialization()
	{
		LoadConfigurationAgents();
		if (!IsPlayerGeneral)
		{
			_assignPlayerRoleInTeamMissioncontroller.OnPlayerChoiceFinalized();
			RefreshAll();
		}
	}

	private void RefreshAll()
	{
		ExecuteClearHeroAndShipSelection();
		_clearFormationSelection?.Invoke();
		RefreshFormations();
		RefreshShips();
		RefreshHeroes();
		RefreshFormationsDisabledAndReason();
		RefreshValues();
		RefreshCanStartMission();
		IsAssignmentDirty = false;
	}

	private void LoadConfigurationShips()
	{
		if (_navalOrderOfBattleCampaignBehavior == null || !IsPlayerGeneral)
		{
			return;
		}
		for (int i = 0; i < AllFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations[i];
			NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData formationInfo = _navalOrderOfBattleCampaignBehavior.GetFormationDataAtIndex(i, MobileParty.MainParty.Army != null);
			if (formationInfo == null || !navalOrderOfBattleFormationItemVM.IsEnabled)
			{
				continue;
			}
			if (formationInfo.Ship != null)
			{
				NavalOrderOfBattleShipItemVM navalOrderOfBattleShipItemVM = _allShips.FirstOrDefault((NavalOrderOfBattleShipItemVM x) => x.ShipOrigin == formationInfo.Ship);
				if (navalOrderOfBattleShipItemVM != null && navalOrderOfBattleShipItemVM.GetCanBeUnassignedOrMoved() && navalOrderOfBattleFormationItemVM.GetCanAcceptShip())
				{
					AssignShipToFormation(navalOrderOfBattleShipItemVM, navalOrderOfBattleFormationItemVM);
					continue;
				}
				NavalOrderOfBattleShipItemVM ship = navalOrderOfBattleFormationItemVM.Ship;
				if (ship == null || ship.GetCanBeUnassignedOrMoved())
				{
					AssignShipToFormation(null, navalOrderOfBattleFormationItemVM);
				}
			}
			else
			{
				NavalOrderOfBattleShipItemVM ship2 = navalOrderOfBattleFormationItemVM.Ship;
				if (ship2 == null || ship2.GetCanBeUnassignedOrMoved())
				{
					AssignShipToFormation(null, navalOrderOfBattleFormationItemVM);
				}
			}
		}
	}

	private void LoadConfigurationAgents()
	{
		if (_navalOrderOfBattleCampaignBehavior == null || !IsPlayerGeneral)
		{
			return;
		}
		_isLoadingConfigurationAgents = true;
		for (int i = 0; i < AllFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations[i];
			NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData formationInfo = _navalOrderOfBattleCampaignBehavior.GetFormationDataAtIndex(i, MobileParty.MainParty.Army != null);
			if (formationInfo == null || !navalOrderOfBattleFormationItemVM.IsEnabled)
			{
				continue;
			}
			if (formationInfo.Captain != null)
			{
				NavalOrderOfBattleHeroItemVM navalOrderOfBattleHeroItemVM = _allHeroes.FirstOrDefault((NavalOrderOfBattleHeroItemVM x) => x.AgentOrigin.Troop == formationInfo.Captain.CharacterObject);
				if (navalOrderOfBattleHeroItemVM != null && navalOrderOfBattleHeroItemVM.GetCanBeUnassignedOrMoved() && navalOrderOfBattleFormationItemVM.GetCanAcceptCaptain())
				{
					AssignCaptainToFormation(navalOrderOfBattleHeroItemVM, navalOrderOfBattleFormationItemVM);
				}
				else
				{
					NavalOrderOfBattleHeroItemVM captain = navalOrderOfBattleFormationItemVM.Captain;
					if (captain == null || captain.GetCanBeUnassignedOrMoved())
					{
						AssignCaptainToFormation(null, navalOrderOfBattleFormationItemVM);
					}
				}
			}
			else
			{
				NavalOrderOfBattleHeroItemVM captain2 = navalOrderOfBattleFormationItemVM.Captain;
				if (captain2 == null || captain2.GetCanBeUnassignedOrMoved())
				{
					AssignCaptainToFormation(null, navalOrderOfBattleFormationItemVM);
				}
			}
			if (formationInfo.FormationClass != DeploymentFormationClass.Unset && navalOrderOfBattleFormationItemVM.IsSelectable)
			{
				if (formationInfo.FormationClass == DeploymentFormationClass.Infantry)
				{
					navalOrderOfBattleFormationItemVM.ExecuteSelectInfantry();
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.Ranged)
				{
					navalOrderOfBattleFormationItemVM.ExecuteSelectRanged();
				}
				else if (formationInfo.FormationClass == DeploymentFormationClass.InfantryAndRanged)
				{
					navalOrderOfBattleFormationItemVM.ExecuteSelectInfantryAndRanged();
				}
				formationInfo.Filters.TryGetValue(FormationFilterType.Shield, out var value);
				formationInfo.Filters.TryGetValue(FormationFilterType.Heavy, out var value2);
				formationInfo.Filters.TryGetValue(FormationFilterType.Thrown, out var value3);
				formationInfo.Filters.TryGetValue(FormationFilterType.HighTier, out var value4);
				formationInfo.Filters.TryGetValue(FormationFilterType.LowTier, out var value5);
				navalOrderOfBattleFormationItemVM.FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Shield).IsActive = value;
				navalOrderOfBattleFormationItemVM.FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Thrown).IsActive = value3;
				navalOrderOfBattleFormationItemVM.FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.Heavy).IsActive = value2;
				navalOrderOfBattleFormationItemVM.FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.HighTier).IsActive = value4;
				navalOrderOfBattleFormationItemVM.FilterItems.FirstOrDefault((OrderOfBattleFormationFilterSelectorItemVM f) => f.FilterType == FormationFilterType.LowTier).IsActive = value5;
			}
		}
		_navalDeploymentController.UpdateShips(TeamSideEnum.PlayerTeam);
		IsAssignmentDirty = true;
		_isLoadingConfigurationAgents = false;
	}

	private void SaveConfiguration()
	{
		if (_navalOrderOfBattleCampaignBehavior == null || !IsPlayerGeneral || !MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
		{
			return;
		}
		List<NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData> list = new List<NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData>();
		for (int i = 0; i < AllFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM formationItemVM = AllFormations[i];
			IShipOrigin shipOrigin = null;
			Hero captain = null;
			bool isSelectable = formationItemVM.IsSelectable;
			if (isSelectable)
			{
				if (formationItemVM.Ship?.ShipOrigin != null && !formationItemVM.Ship.IsDisabled)
				{
					shipOrigin = formationItemVM.Ship.ShipOrigin;
				}
				if (formationItemVM.Captain?.AgentOrigin != null && !formationItemVM.Captain.IsDisabled)
				{
					captain = Hero.FindFirst((Hero h) => h.CharacterObject == formationItemVM.Captain.AgentOrigin.Troop);
				}
			}
			DeploymentFormationClass formationClass = (isSelectable ? ((DeploymentFormationClass)formationItemVM.FormationClassInt) : DeploymentFormationClass.Unset);
			Dictionary<FormationFilterType, bool> filters = new Dictionary<FormationFilterType, bool>
			{
				[FormationFilterType.Shield] = isSelectable && formationItemVM.HasFilter(FormationFilterType.Shield),
				[FormationFilterType.Thrown] = isSelectable && formationItemVM.HasFilter(FormationFilterType.Thrown),
				[FormationFilterType.Heavy] = isSelectable && formationItemVM.HasFilter(FormationFilterType.Heavy),
				[FormationFilterType.HighTier] = isSelectable && formationItemVM.HasFilter(FormationFilterType.HighTier),
				[FormationFilterType.LowTier] = isSelectable && formationItemVM.HasFilter(FormationFilterType.LowTier)
			};
			list.Add(new NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData(captain, shipOrigin as Ship, formationClass, filters));
		}
		_navalOrderOfBattleCampaignBehavior.SetFormationInfos(list, MobileParty.MainParty.Army != null);
	}

	private void OnClassChanged(NavalOrderOfBattleFormationItemVM formationItem)
	{
		if (!IsAssignmentDirty)
		{
			TroopTraitsMask filter = TroopFilteringUtilities.GetFilter(formationItem.SelectedClass.GetFormationClasses().ToArray());
			if (_navalDeploymentController.SetTroopClassFilter(filter, formationItem.Formation, !_isLoadingConfigurationAgents))
			{
				IsAssignmentDirty = true;
			}
		}
	}

	private void OnFilterUseToggled(NavalOrderOfBattleFormationItemVM formationItem)
	{
		if (!IsAssignmentDirty)
		{
			TroopTraitsMask filter = TroopFilteringUtilities.GetFilter((from f in formationItem.FilterItems
				where f.IsActive
				select f.FilterType).ToArray());
			if (_navalDeploymentController.SetTroopTraitsFilter(filter, formationItem.Formation, !_isLoadingConfigurationAgents))
			{
				IsAssignmentDirty = true;
			}
		}
	}

	private void OnFormationSelected(NavalOrderOfBattleFormationItemVM formation)
	{
		if (!IsAssignmentDirty)
		{
			_onFormationSelected?.Invoke(formation);
			ExecuteClearHeroAndShipSelection();
		}
	}

	private void OnSelectedFormationsChanged()
	{
		for (int i = 0; i < AllFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations[i];
			navalOrderOfBattleFormationItemVM.IsSelected = _orderController.IsFormationListening(navalOrderOfBattleFormationItemVM.Formation);
		}
	}

	private void OnShipSelected(NavalOrderOfBattleShipItemVM ship, bool isSelected)
	{
		if (!IsAssignmentDirty)
		{
			if (isSelected)
			{
				SelectedShip = ship;
				SelectedHero = null;
				_clearFormationSelection?.Invoke();
			}
			else if (SelectedShip == ship)
			{
				SelectedShip = null;
			}
			else
			{
				Debug.FailedAssert("Trying to deselect ship that isn't SelectedShip!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "OnShipSelected", 795);
			}
		}
	}

	private void OnHeroSelected(NavalOrderOfBattleHeroItemVM hero, bool isSelected)
	{
		if (!IsAssignmentDirty)
		{
			if (isSelected)
			{
				SelectedHero = hero;
				SelectedShip = null;
				_clearFormationSelection?.Invoke();
			}
			else if (SelectedHero == hero)
			{
				SelectedHero = null;
			}
			else
			{
				Debug.FailedAssert("Trying to deselect hero that isn't SelectedHero!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "OnHeroSelected", 820);
			}
		}
	}

	private void OnFormationAcceptCaptain(NavalOrderOfBattleFormationItemVM formation)
	{
		if (!IsAssignmentDirty)
		{
			if (SelectedHero != null)
			{
				AssignCaptainToFormation(SelectedHero, formation);
				SelectedHero = null;
			}
			else
			{
				Debug.FailedAssert("OnFormationAcceptCaptain called without a selected hero!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "OnFormationAcceptCaptain", 838);
			}
		}
	}

	private void OnFormationAcceptShip(NavalOrderOfBattleFormationItemVM formation)
	{
		if (!IsAssignmentDirty)
		{
			if (SelectedShip != null)
			{
				AssignShipToFormation(SelectedShip, formation);
				SelectedShip = null;
			}
			else
			{
				Debug.FailedAssert("OnFormationAcceptShip called without a selected ship!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "OnFormationAcceptShip", 856);
			}
		}
	}

	public void ExecuteReturnHeroToPool()
	{
		if (IsAssignmentDirty)
		{
			return;
		}
		if (SelectedHero != null)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = FindFormationOfCaptain(SelectedHero);
			if (navalOrderOfBattleFormationItemVM != null)
			{
				AssignCaptainToFormation(null, navalOrderOfBattleFormationItemVM);
			}
			SelectedHero = null;
		}
		else
		{
			Debug.FailedAssert("ExecuteReturnHeroToPool called without a selected hero!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "ExecuteReturnHeroToPool", 879);
		}
	}

	public void ExecuteReturnShipToPool()
	{
		if (IsAssignmentDirty)
		{
			return;
		}
		if (SelectedShip != null)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = FindFormationOfShip(SelectedShip);
			if (navalOrderOfBattleFormationItemVM != null)
			{
				AssignShipToFormation(null, navalOrderOfBattleFormationItemVM);
			}
			SelectedShip = null;
		}
		else
		{
			Debug.FailedAssert("ExecuteReturnShipToPool called without a selected ship!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "ExecuteReturnShipToPool", 902);
		}
	}

	private void AssignCaptainToFormation(NavalOrderOfBattleHeroItemVM hero, NavalOrderOfBattleFormationItemVM formation)
	{
		if (formation == null)
		{
			Debug.FailedAssert("Trying to assign hero to null formation!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "AssignCaptainToFormation", 910);
			return;
		}
		bool flag = false;
		if (_navalDeploymentController.IsShipAssignedToFormation(formation.Formation))
		{
			flag = _navalDeploymentController.TryAssignCaptainToFormation(hero?.AgentOrigin, formation.Formation);
		}
		if (flag)
		{
			RefreshAll();
		}
	}

	private bool AssignShipToFormation(NavalOrderOfBattleShipItemVM ship, NavalOrderOfBattleFormationItemVM formation, bool isBatch = false)
	{
		if (formation == null)
		{
			Debug.FailedAssert("Trying to assign ship to null formation!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\OrderOfBattle\\NavalOrderOfBattleVM.cs", "AssignShipToFormation", 936);
			return false;
		}
		bool num = _navalDeploymentController.TryAssignShipToFormation(ship?.ShipOrigin, formation.Formation, !isBatch);
		if (num)
		{
			IsAssignmentDirty = true;
		}
		return num;
	}

	private void OnSelectionUpdated()
	{
		for (int i = 0; i < AllFormations.Count; i++)
		{
			NavalOrderOfBattleFormationItemVM navalOrderOfBattleFormationItemVM = AllFormations[i];
			navalOrderOfBattleFormationItemVM.IsAcceptingCaptain = HasSelectedHero && SelectedHero != navalOrderOfBattleFormationItemVM.Captain && navalOrderOfBattleFormationItemVM.GetCanAcceptCaptain() && SelectedHero.GetCanBeUnassignedOrMoved();
			navalOrderOfBattleFormationItemVM.IsAcceptingShip = HasSelectedShip && SelectedShip != navalOrderOfBattleFormationItemVM.Ship && navalOrderOfBattleFormationItemVM.GetCanAcceptShip() && SelectedShip.GetCanBeUnassignedOrMoved();
		}
		IsPoolAcceptingHero = HasSelectedHero && !UnassignedHeroes.Contains(SelectedHero) && SelectedHero.GetCanBeUnassignedOrMoved();
		IsPoolAcceptingShip = HasSelectedShip && !UnassignedShips.Contains(SelectedShip) && SelectedShip.GetCanBeUnassignedOrMoved();
	}

	private void OnPlayerShipsUpdated()
	{
		RefreshAll();
		if (_finalizeInitializationOnNextUpdate)
		{
			FinalizeInitialization();
			_finalizeInitializationOnNextUpdate = false;
		}
	}

	private NavalOrderOfBattleFormationItemVM FindFormationOfCaptain(NavalOrderOfBattleHeroItemVM hero)
	{
		for (int i = 0; i < AllFormations.Count; i++)
		{
			if (AllFormations[i].Captain == hero)
			{
				return AllFormations[i];
			}
		}
		return null;
	}

	private NavalOrderOfBattleFormationItemVM FindFormationOfShip(NavalOrderOfBattleShipItemVM ship)
	{
		for (int i = 0; i < AllFormations.Count; i++)
		{
			if (AllFormations[i].Ship == ship)
			{
				return AllFormations[i];
			}
		}
		return null;
	}

	private int GetTroopCountWithFilter(DeploymentFormationClass orderOfBattleFormationClass, FormationFilterType filterType)
	{
		int num = 0;
		List<FormationClass> formationClasses = orderOfBattleFormationClass.GetFormationClasses();
		foreach (NavalOrderOfBattleFormationItemVM allFormation in AllFormations)
		{
			List<FormationClass> formationClasses2 = allFormation.SelectedClass.GetFormationClasses();
			if (!formationClasses.Intersect(formationClasses2).Any())
			{
				continue;
			}
			switch (filterType)
			{
			case FormationFilterType.Shield:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.HasShieldCached);
				break;
			case FormationFilterType.Thrown:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.HasThrownCached);
				break;
			case FormationFilterType.Heavy:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => MissionGameModels.Current.AgentStatCalculateModel.HasHeavyArmor(a));
				break;
			case FormationFilterType.HighTier:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.Character.GetBattleTier() >= 4);
				break;
			case FormationFilterType.LowTier:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.Character.GetBattleTier() <= 3);
				break;
			}
		}
		return num;
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
