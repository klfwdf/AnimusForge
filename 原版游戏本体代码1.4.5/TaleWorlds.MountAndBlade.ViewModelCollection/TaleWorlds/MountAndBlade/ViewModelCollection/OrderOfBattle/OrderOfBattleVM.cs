using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleVM : ViewModel
{
	private readonly TextObject _bannerText = new TextObject("{=FvYhaE3z}Banner");

	private readonly TextObject _bannerEffectText = new TextObject("{=zjcZZgUY}Banner Effect");

	private readonly TextObject _noBannerEquippedText = new TextObject("{=suyl7WWa}No banner equipped");

	private readonly TextObject _missingFormationsHintText = new TextObject("{=2AGvFYk9}To start the mission, you need to have at least one formation with {FORMATION_CLASS} class.");

	private readonly TextObject _selectAllHintText = new TextObject("{=YwbymaBc}Select all heroes");

	private bool _isSaving;

	private readonly TextObject _clearSelectionHintText = new TextObject("{=Sbb8YcJM}Deselect all selected heroes");

	private Dictionary<FormationClass, int> _visibleTroopTypeCountLookup;

	private bool _isUnitDeployRefreshed;

	private Action<int> _selectFormationAtIndex;

	private readonly List<OrderOfBattleHeroItemVM> _selectedHeroes;

	private Action<int> _deselectFormationAtIndex;

	protected readonly List<OrderOfBattleHeroItemVM> _allHeroes;

	private List<FormationClass> _availableTroopTypes;

	private bool _isInitialized;

	protected List<OrderOfBattleFormationItemVM> _allFormations;

	private Action _clearFormationSelection;

	private Action _onAutoDeploy;

	private Action _onBeginMission;

	private Mission _mission;

	private Camera _missionCamera;

	private BannerBearerLogic _bannerBearerLogic;

	private OrderController _orderController;

	private bool _isMissingFormationsDirty;

	private bool _isHeroSelectionDirty;

	private bool _isTroopCountsDirty;

	private OrderOfBattleFormationItemVM _lastEnabledClassSelection;

	private bool _isEnabled;

	private bool _isPlayerGeneral;

	private bool _areCameraControlsEnabled;

	private bool _canStartMission = true;

	private bool _isPoolAcceptingCaptain;

	private bool _isPoolAcceptingHeroTroops;

	private bool _isPoolAcceptingAny;

	private string _beginMissionText;

	private bool _hasSelectedHeroes;

	private int _selectedHeroCount;

	private bool _areHotkeysEnabled = true;

	private MBBindingList<OrderOfBattleFormationItemVM> _formationsSecondHalf;

	private HintViewModel _missingFormationsHint;

	private HintViewModel _selectAllHint;

	private HintViewModel _clearSelectionHint;

	private bool _canToggleHeroSelection;

	private string _autoDeployText;

	private MBBindingList<OrderOfBattleHeroItemVM> _unassignedHeroes;

	private OrderOfBattleHeroItemVM _lastSelectedHeroItem;

	private MBBindingList<OrderOfBattleFormationItemVM> _formationsFirstHalf;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _resetInputKey;

	private string _latestTutorialElementID;

	private const string _assignCaptainHighlightID = "AssignCaptain";

	private const string _createFormationHighlightID = "CreateFormation";

	private bool _isAssignCaptainHighlightApplied;

	private bool _isCreateFormationHighlightApplied;

	protected int TotalFormationCount => _mission.PlayerTeam.FormationsIncludingEmpty.Count;

	public List<MissionOrderVM.FormationConfiguration> CurrentConfiguration { get; private set; }

	[DataSourceProperty]
	public bool IsPoolAcceptingHeroTroops
	{
		get
		{
			return _isPoolAcceptingHeroTroops;
		}
		set
		{
			if (value != _isPoolAcceptingHeroTroops)
			{
				_isPoolAcceptingHeroTroops = value;
				OnPropertyChangedWithValue(value, "IsPoolAcceptingHeroTroops");
				IsPoolAcceptingAny = IsPoolAcceptingCaptain || IsPoolAcceptingHeroTroops;
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
	public bool HasSelectedHeroes
	{
		get
		{
			return _hasSelectedHeroes;
		}
		set
		{
			if (value != _hasSelectedHeroes)
			{
				_hasSelectedHeroes = value;
				OnPropertyChangedWithValue(value, "HasSelectedHeroes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderOfBattleFormationItemVM> FormationsFirstHalf
	{
		get
		{
			return _formationsFirstHalf;
		}
		set
		{
			if (value != _formationsFirstHalf)
			{
				_formationsFirstHalf = value;
				OnPropertyChangedWithValue(value, "FormationsFirstHalf");
			}
		}
	}

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
	public bool IsPoolAcceptingCaptain
	{
		get
		{
			return _isPoolAcceptingCaptain;
		}
		set
		{
			if (value != _isPoolAcceptingCaptain)
			{
				_isPoolAcceptingCaptain = value;
				OnPropertyChangedWithValue(value, "IsPoolAcceptingCaptain");
				IsPoolAcceptingAny = IsPoolAcceptingCaptain || IsPoolAcceptingHeroTroops;
			}
		}
	}

	[DataSourceProperty]
	public bool IsPoolAcceptingAny
	{
		get
		{
			return _isPoolAcceptingAny;
		}
		set
		{
			if (value != _isPoolAcceptingAny)
			{
				_isPoolAcceptingAny = value;
				OnPropertyChangedWithValue(value, "IsPoolAcceptingAny");
			}
		}
	}

	[DataSourceProperty]
	public int SelectedHeroCount
	{
		get
		{
			return _selectedHeroCount;
		}
		set
		{
			if (value != _selectedHeroCount)
			{
				_selectedHeroCount = value;
				OnPropertyChangedWithValue(value, "SelectedHeroCount");
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
	public HintViewModel ClearSelectionHint
	{
		get
		{
			return _clearSelectionHint;
		}
		set
		{
			if (value != _clearSelectionHint)
			{
				_clearSelectionHint = value;
				OnPropertyChangedWithValue(value, "ClearSelectionHint");
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
	public HintViewModel SelectAllHint
	{
		get
		{
			return _selectAllHint;
		}
		set
		{
			if (value != _selectAllHint)
			{
				_selectAllHint = value;
				OnPropertyChangedWithValue(value, "SelectAllHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MissingFormationsHint
	{
		get
		{
			return _missingFormationsHint;
		}
		set
		{
			if (value != _missingFormationsHint)
			{
				_missingFormationsHint = value;
				OnPropertyChangedWithValue(value, "MissingFormationsHint");
			}
		}
	}

	[DataSourceProperty]
	public OrderOfBattleHeroItemVM LastSelectedHeroItem
	{
		get
		{
			return _lastSelectedHeroItem;
		}
		set
		{
			if (value != _lastSelectedHeroItem)
			{
				_lastSelectedHeroItem = value;
				OnPropertyChangedWithValue(value, "LastSelectedHeroItem");
			}
		}
	}

	[DataSourceProperty]
	public bool CanToggleHeroSelection
	{
		get
		{
			return _canToggleHeroSelection;
		}
		set
		{
			if (value != _canToggleHeroSelection)
			{
				_canToggleHeroSelection = value;
				OnPropertyChangedWithValue(value, "CanToggleHeroSelection");
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

	[DataSourceProperty]
	public MBBindingList<OrderOfBattleFormationItemVM> FormationsSecondHalf
	{
		get
		{
			return _formationsSecondHalf;
		}
		set
		{
			if (value != _formationsSecondHalf)
			{
				_formationsSecondHalf = value;
				OnPropertyChangedWithValue(value, "FormationsSecondHalf");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderOfBattleHeroItemVM> UnassignedHeroes
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

	public OrderOfBattleVM()
	{
		_allFormations = new List<OrderOfBattleFormationItemVM>();
		_allHeroes = new List<OrderOfBattleHeroItemVM>();
		_selectedHeroes = new List<OrderOfBattleHeroItemVM>();
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		BeginMissionText = new TextObject("{=SYYOSOoa}Ready").ToString();
		Mission mission = _mission;
		if (mission != null && mission.IsSiegeBattle)
		{
			AutoDeployText = GameTexts.FindText("str_auto_deploy").ToString();
		}
		else
		{
			AutoDeployText = new TextObject("{=ADKHovtz}Reset Deployment").ToString();
		}
		MissingFormationsHint = new HintViewModel(_missingFormationsHintText);
		SelectAllHint = new HintViewModel(_selectAllHintText);
		ClearSelectionHint = new HintViewModel(_clearSelectionHintText);
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.RefreshValues();
		});
		UnassignedHeroes?.ApplyActionOnAllItems(delegate(OrderOfBattleHeroItemVM c)
		{
			c.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		FinalizeFormationCallbacks();
		DoneInputKey?.OnFinalize();
		ResetInputKey?.OnFinalize();
	}

	private void InitializeFormationCallbacks()
	{
		OrderOfBattleFormationItemVM.OnClassSelectionToggled = OnClassSelectionToggled;
		OrderOfBattleFormationItemVM.OnHeroesChanged = OnHeroesChanged;
		OrderOfBattleFormationItemVM.OnFilterUseToggled = OnFilterUseToggled;
		OrderOfBattleFormationItemVM.OnSelection = SelectFormationItem;
		OrderOfBattleFormationItemVM.OnDeselection = DeselectFormationItem;
		OrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter = GetTroopCountWithFilter;
		OrderOfBattleFormationItemVM.GetFormationWithCondition = GetFormationItemsWithCondition;
		OrderOfBattleFormationItemVM.HasAnyTroopWithClass = HasAnyTroopWithClass;
		OrderOfBattleFormationItemVM.OnAcceptCaptain = OnFormationAcceptCaptain;
		OrderOfBattleFormationItemVM.OnAcceptHeroTroops = OnFormationAcceptHeroTroops;
		OrderOfBattleFormationItemVM.OnFormationClassChanged = RefreshWeights;
		OrderOfBattleFormationClassVM.OnWeightAdjustedCallback = OnWeightAdjusted;
		OrderOfBattleFormationClassVM.OnClassChanged = OnFormationClassChanged;
		OrderOfBattleFormationClassVM.CanAdjustWeight = CanAdjustWeight;
		OrderOfBattleFormationClassVM.GetTotalCountOfTroopType = GetVisibleTotalTroopCountOfType;
		OrderOfBattleHeroItemVM.OnHeroAssignmentBegin = OnHeroAssignmentBegin;
		OrderOfBattleHeroItemVM.OnHeroAssignmentEnd = OnHeroAssignmentEnd;
		OrderOfBattleHeroItemVM.GetAgentTooltip = GetAgentTooltip;
		OrderOfBattleHeroItemVM.OnHeroSelection = OnHeroSelection;
		OrderOfBattleHeroItemVM.OnHeroAssignedFormationChanged = OnHeroAssignedFormationChanged;
	}

	private void FinalizeFormationCallbacks()
	{
		OrderOfBattleFormationItemVM.OnClassSelectionToggled = null;
		OrderOfBattleFormationItemVM.OnHeroesChanged = null;
		OrderOfBattleFormationItemVM.OnFilterUseToggled = null;
		OrderOfBattleFormationItemVM.OnSelection = null;
		OrderOfBattleFormationItemVM.OnDeselection = null;
		OrderOfBattleFormationItemVM.GetTotalTroopCountWithFilter = null;
		OrderOfBattleFormationItemVM.GetFormationWithCondition = null;
		OrderOfBattleFormationItemVM.HasAnyTroopWithClass = null;
		OrderOfBattleFormationItemVM.OnAcceptCaptain = null;
		OrderOfBattleFormationItemVM.OnAcceptHeroTroops = null;
		OrderOfBattleFormationItemVM.OnFormationClassChanged = null;
		OrderOfBattleFormationClassVM.OnWeightAdjustedCallback = null;
		OrderOfBattleFormationClassVM.OnClassChanged = null;
		OrderOfBattleFormationClassVM.CanAdjustWeight = null;
		OrderOfBattleFormationClassVM.GetTotalCountOfTroopType = null;
		OrderOfBattleHeroItemVM.OnHeroAssignmentBegin = null;
		OrderOfBattleHeroItemVM.OnHeroAssignmentEnd = null;
		OrderOfBattleHeroItemVM.GetAgentTooltip = null;
		OrderOfBattleHeroItemVM.OnHeroSelection = null;
		OrderOfBattleHeroItemVM.OnHeroAssignedFormationChanged = null;
	}

	public void Tick()
	{
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation?.Tick();
			if (allFormation != null)
			{
				EnsureAllFormationTypesAreSet(allFormation);
			}
		}
		if (_isInitialized)
		{
			if (_isHeroSelectionDirty)
			{
				UpdateHeroItemSelection();
				_isHeroSelectionDirty = false;
			}
			if (_isTroopCountsDirty)
			{
				UpdateTroopTypeLookUpTable();
				_isTroopCountsDirty = false;
			}
			if (_isMissingFormationsDirty)
			{
				RefreshMissingFormations();
				_isMissingFormationsDirty = false;
			}
			if (!_isUnitDeployRefreshed)
			{
				OnUnitDeployed();
				_isUnitDeployRefreshed = true;
			}
		}
	}

	private void EnsureAllFormationTypesAreSet(OrderOfBattleFormationItemVM f)
	{
		if (!IsPlayerGeneral || f.OrderOfBattleFormationClassInt != 0 || f.Formation.CountOfUnits <= 0)
		{
			return;
		}
		bool oldValue = _orderController.BackupAndDisableGesturesEnabled();
		for (int i = 0; i < _allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
			if (_orderController.SelectedFormations.Contains(orderOfBattleFormationItemVM?.Formation))
			{
				_orderController.DeselectFormation(orderOfBattleFormationItemVM?.Formation);
			}
		}
		OrderOfBattleFormationItemVM orderOfBattleFormationItemVM2 = _allFormations.Find((OrderOfBattleFormationItemVM other) => other.Classes.Any((OrderOfBattleFormationClassVM fc) => fc.Class == f.Formation.PhysicalClass));
		if (orderOfBattleFormationItemVM2 == null)
		{
			if (_mission.IsSiegeBattle && (f.Formation.PhysicalClass == FormationClass.Cavalry || f.Formation.PhysicalClass == FormationClass.HorseArcher))
			{
				FormationClass secondaryFormationClassForMountedFormation = ((f.Formation.PhysicalClass != FormationClass.Cavalry) ? FormationClass.Ranged : FormationClass.Infantry);
				orderOfBattleFormationItemVM2 = _allFormations.Find((OrderOfBattleFormationItemVM other) => other.Classes.Any((OrderOfBattleFormationClassVM fc) => fc.Class == secondaryFormationClassForMountedFormation));
			}
			if (orderOfBattleFormationItemVM2 == null)
			{
				orderOfBattleFormationItemVM2 = _allFormations.Find((OrderOfBattleFormationItemVM other) => other.OrderOfBattleFormationClassInt != 0);
			}
		}
		if (orderOfBattleFormationItemVM2 == null)
		{
			return;
		}
		Formation formation = orderOfBattleFormationItemVM2.Formation;
		_orderController.SelectFormation(f.Formation);
		_orderController.SetOrderWithFormationAndNumber(OrderType.Transfer, formation, f.Formation.CountOfUnits);
		for (int num = 0; num < _allFormations.Count; num++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM3 = _allFormations[num];
			if (_orderController.SelectedFormations.Contains(orderOfBattleFormationItemVM3.Formation))
			{
				_orderController.DeselectFormation(orderOfBattleFormationItemVM3.Formation);
			}
		}
		orderOfBattleFormationItemVM2.OnSizeChanged();
		f.OnSizeChanged();
		RefreshWeights();
		_orderController.RestoreGesturesEnabled(oldValue);
	}

	public void Initialize(Mission mission, Camera missionCamera, Action<int> selectFormationAtIndex, Action<int> deselectFormationAtIndex, Action clearFormationSelection, Action onAutoDeploy, Action onBeginMission, Dictionary<int, Agent> formationIndicesAndSergeants)
	{
		_mission = mission;
		_missionCamera = missionCamera;
		_selectFormationAtIndex = selectFormationAtIndex;
		_deselectFormationAtIndex = deselectFormationAtIndex;
		_clearFormationSelection = clearFormationSelection;
		_onAutoDeploy = onAutoDeploy;
		_onBeginMission = onBeginMission;
		_bannerBearerLogic = mission.GetMissionBehavior<BannerBearerLogic>();
		if (_bannerBearerLogic != null)
		{
			_bannerBearerLogic.OnBannerBearersUpdated += OnBannerBearersUpdated;
			_bannerBearerLogic.OnBannerBearerAgentUpdated += OnBannerAgentUpdated;
		}
		InitializeFormationCallbacks();
		_isInitialized = false;
		_orderController = Mission.Current.PlayerTeam.PlayerOrderController;
		_orderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
		_orderController.OnOrderIssued += OnOrderIssued;
		CurrentConfiguration = new List<MissionOrderVM.FormationConfiguration>();
		_availableTroopTypes = MissionGameModels.Current.BattleInitializationModel.GetAllAvailableTroopTypes();
		IsPlayerGeneral = _mission.PlayerTeam.IsPlayerGeneral;
		FormationsFirstHalf = new MBBindingList<OrderOfBattleFormationItemVM>();
		FormationsSecondHalf = new MBBindingList<OrderOfBattleFormationItemVM>();
		UnassignedHeroes = new MBBindingList<OrderOfBattleHeroItemVM>();
		_visibleTroopTypeCountLookup = new Dictionary<FormationClass, int>
		{
			{
				FormationClass.Infantry,
				0
			},
			{
				FormationClass.Ranged,
				0
			},
			{
				FormationClass.Cavalry,
				0
			},
			{
				FormationClass.HorseArcher,
				0
			}
		};
		for (int i = 0; i < TotalFormationCount; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = new OrderOfBattleFormationItemVM(_missionCamera);
			if (i < TotalFormationCount / 2)
			{
				FormationsFirstHalf.Add(orderOfBattleFormationItemVM);
			}
			else
			{
				FormationsSecondHalf.Add(orderOfBattleFormationItemVM);
			}
			_allFormations.Add(orderOfBattleFormationItemVM);
			Formation formation = _mission.PlayerTeam.FormationsIncludingEmpty.ElementAt(i);
			orderOfBattleFormationItemVM.RefreshFormation(formation);
		}
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.OnSizeChanged();
		});
		foreach (Agent heroAgent in _mission.PlayerTeam.GetHeroAgents())
		{
			_allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => heroAgent.Formation == f.Formation);
			OrderOfBattleHeroItemVM item = new OrderOfBattleHeroItemVM(heroAgent);
			_allHeroes.Add(item);
			if (IsPlayerGeneral || heroAgent.IsMainAgent)
			{
				UnassignedHeroes.Add(item);
			}
		}
		if (!IsPlayerGeneral)
		{
			foreach (KeyValuePair<int, Agent> preAssignedCaptain in formationIndicesAndSergeants)
			{
				_allHeroes.First((OrderOfBattleHeroItemVM h) => h.Agent == preAssignedCaptain.Value).SetIsPreAssigned(isPreAssigned: true);
				AssignCaptain(preAssignedCaptain.Value, _allFormations[preAssignedCaptain.Key]);
			}
		}
		IsEnabled = true;
		SetAllFormationsLockState(isLocked: true);
		LoadConfiguration();
		SetAllFormationsLockState(isLocked: false);
		SetInitialHeroFormations();
		DistributeAllTroops();
		_isInitialized = true;
		RefreshWeights();
		DeselectAllFormations();
		OnUnitDeployed();
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.UpdateAdjustable();
		});
		if (!IsPlayerGeneral)
		{
			SelectHeroItem(_allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM h) => h.Agent.IsMainAgent));
		}
		_isMissingFormationsDirty = true;
		_isTroopCountsDirty = true;
		RefreshValues();
	}

	private void UpdateTroopTypeLookUpTable()
	{
		for (FormationClass formationClass = FormationClass.Infantry; formationClass < FormationClass.NumberOfDefaultFormations; formationClass++)
		{
			_visibleTroopTypeCountLookup[formationClass] = 0;
		}
		for (int i = 0; i < _allFormations.Count; i++)
		{
			Formation formation = _allFormations[i].Formation;
			if (formation != null)
			{
				for (FormationClass formationClass2 = FormationClass.Infantry; formationClass2 < FormationClass.NumberOfDefaultFormations; formationClass2++)
				{
					_visibleTroopTypeCountLookup[formationClass2] += formation.GetCountOfUnitsBelongingToPhysicalClass(formationClass2, excludeBannerBearers: false);
				}
			}
		}
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation.OnSizeChanged();
		}
	}

	private void SetAllFormationsLockState(bool isLocked)
	{
		for (int i = 0; i < _allFormations.Count; i++)
		{
			for (int j = 0; j < _allFormations[i].Classes.Count; j++)
			{
				_allFormations[i].Classes[j].SetWeightAdjustmentLock(isLocked);
			}
		}
	}

	private void OnBannerBearersUpdated(Formation formation)
	{
		if (!_isInitialized)
		{
			return;
		}
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation.Formation.QuerySystem.Expire();
		}
		_isTroopCountsDirty = true;
	}

	private void OnBannerAgentUpdated(Agent agent, bool isBannerBearer)
	{
		if (_isInitialized && (agent.Team.IsPlayerTeam || agent.Team.IsPlayerAlly) && _orderController.SelectedFormations.Contains(agent.Formation))
		{
			_orderController.DeselectFormation(agent.Formation);
			_orderController.SelectFormation(agent.Formation);
		}
	}

	private OrderOfBattleFormationItemVM GetFirstAvailableFormationWithAnyClass(params FormationClass[] classes)
	{
		int i;
		for (i = 0; i < classes.Length; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => f.HasClass(classes[i]));
			if (orderOfBattleFormationItemVM != null)
			{
				return orderOfBattleFormationItemVM;
			}
		}
		return null;
	}

	private OrderOfBattleFormationItemVM GetInitialHeroFormation(OrderOfBattleHeroItemVM hero)
	{
		FormationClass heroClass = FormationClass.NumberOfAllFormations;
		for (FormationClass formationClass = FormationClass.Infantry; formationClass < FormationClass.NumberOfDefaultFormations; formationClass++)
		{
			if (OrderOfBattleUIHelper.IsAgentInFormationClass(hero.Agent, formationClass))
			{
				heroClass = formationClass;
			}
		}
		if (heroClass == FormationClass.NumberOfAllFormations)
		{
			return null;
		}
		OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = null;
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			if (allFormation.Captain.Agent == hero.Agent || allFormation.HeroTroops.Contains(hero))
			{
				hero.Agent.Formation = allFormation.Formation;
				return allFormation;
			}
			if (allFormation.Formation == hero.Agent.Formation)
			{
				for (int num = allFormation.Classes.Count - 1; num >= 0; num--)
				{
					if (!allFormation.Classes[num].IsUnset && allFormation.Classes[num].Class == heroClass)
					{
						return allFormation;
					}
				}
			}
			if (orderOfBattleFormationItemVM != null)
			{
				break;
			}
		}
		if (!UnassignedHeroes.Contains(hero))
		{
			UnassignedHeroes.Add(hero);
		}
		OrderOfBattleFormationItemVM orderOfBattleFormationItemVM2 = _allFormations.FirstOrDefault((OrderOfBattleFormationItemVM x) => x.Classes.Any((OrderOfBattleFormationClassVM c) => c.Class == heroClass));
		if (orderOfBattleFormationItemVM2 != null)
		{
			hero.Agent.Formation = orderOfBattleFormationItemVM2.Formation;
			return orderOfBattleFormationItemVM2;
		}
		FormationClass[] array = null;
		if (heroClass == FormationClass.HorseArcher)
		{
			array = new FormationClass[3]
			{
				FormationClass.Ranged,
				FormationClass.Cavalry,
				FormationClass.Infantry
			};
		}
		else if (heroClass == FormationClass.Cavalry)
		{
			array = new FormationClass[2]
			{
				FormationClass.Infantry,
				FormationClass.Ranged
			};
		}
		else if (heroClass == FormationClass.Ranged)
		{
			array = new FormationClass[1];
		}
		if (array != null)
		{
			OrderOfBattleFormationItemVM firstAvailableFormationWithAnyClass = GetFirstAvailableFormationWithAnyClass(array);
			if (firstAvailableFormationWithAnyClass != null)
			{
				hero.Agent.Formation = firstAvailableFormationWithAnyClass.Formation;
				return firstAvailableFormationWithAnyClass;
			}
		}
		return null;
	}

	private List<(OrderOfBattleHeroItemVM Hero, bool WasCaptain)> ClearAllHeroAssignments()
	{
		List<(OrderOfBattleHeroItemVM, bool)> list = new List<(OrderOfBattleHeroItemVM, bool)>();
		for (int i = 0; i < _allFormations.Count; i++)
		{
			if (_allFormations[i].HasCaptain)
			{
				OrderOfBattleHeroItemVM captain = _allFormations[i].Captain;
				list.Add((captain, true));
				ClearHeroAssignment(captain);
			}
			for (int num = _allFormations[i].HeroTroops.Count - 1; num >= 0; num--)
			{
				OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = _allFormations[i].HeroTroops[num];
				list.Add((orderOfBattleHeroItemVM, false));
				ClearHeroAssignment(orderOfBattleHeroItemVM);
			}
		}
		return list;
	}

	private void SetInitialHeroFormations()
	{
		for (int i = 0; i < _allHeroes.Count; i++)
		{
			OrderOfBattleFormationItemVM initialHeroFormation = GetInitialHeroFormation(_allHeroes[i]);
			if (initialHeroFormation != null)
			{
				_allHeroes[i].SetInitialFormation(initialHeroFormation);
				continue;
			}
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => f.HasFormation && f.Classes.Any((OrderOfBattleFormationClassVM c) => !c.IsUnset));
			if (orderOfBattleFormationItemVM != null)
			{
				_allHeroes[i].SetInitialFormation(orderOfBattleFormationItemVM);
			}
			else
			{
				Debug.FailedAssert("Failed to find an initial formation for hero: " + _allHeroes[i].Agent.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "SetInitialHeroFormations", 608);
			}
		}
	}

	protected virtual void LoadConfiguration()
	{
	}

	protected virtual void SaveConfiguration()
	{
	}

	protected virtual List<TooltipProperty> GetAgentTooltip(Agent agent)
	{
		if (agent == null)
		{
			return new List<TooltipProperty>
			{
				new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator)
			};
		}
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(agent.Name, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		if (agent.FormationBanner != null && agent.FormationBanner.ItemComponent is BannerComponent bannerComponent)
		{
			if (!TextObject.IsNullOrEmpty(agent.FormationBanner.Name))
			{
				list.Add(new TooltipProperty(_bannerText.ToString(), agent.FormationBanner.Name.ToString(), 0));
				GameTexts.SetVariable("RANK", bannerComponent.BannerEffect.Name);
				string content = string.Empty;
				if (bannerComponent.BannerEffect.IncrementType == EffectIncrementType.AddFactor)
				{
					TextObject textObject = GameTexts.FindText("str_NUMBER_percent");
					textObject.SetTextVariable("NUMBER", ((int)Math.Abs(bannerComponent.GetBannerEffectBonus() * 100f)).ToString());
					content = textObject.ToString();
				}
				else if (bannerComponent.BannerEffect.IncrementType == EffectIncrementType.Add)
				{
					content = bannerComponent.GetBannerEffectBonus().ToString();
				}
				GameTexts.SetVariable("NUMBER", content);
				list.Add(new TooltipProperty(_bannerEffectText.ToString(), GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString(), 0));
			}
			else
			{
				Debug.FailedAssert("Agent's formation banner name should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "GetAgentTooltip", 670);
			}
		}
		else
		{
			list.Add(new TooltipProperty(_noBannerEquippedText.ToString(), string.Empty, 0));
		}
		return list;
	}

	private bool HasAnyTroopWithClass(FormationClass formationClass)
	{
		return _availableTroopTypes.Contains(formationClass);
	}

	private void RefreshWeights()
	{
		if (_isSaving || !_isInitialized)
		{
			return;
		}
		List<OrderOfBattleFormationClassVM> list = new List<OrderOfBattleFormationClassVM>();
		for (int i = 0; i < _allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
			for (int j = 0; j < orderOfBattleFormationItemVM.Classes.Count; j++)
			{
				OrderOfBattleFormationClassVM orderOfBattleFormationClassVM = orderOfBattleFormationItemVM.Classes[j];
				if (orderOfBattleFormationClassVM.Class != FormationClass.NumberOfAllFormations)
				{
					list.Add(orderOfBattleFormationClassVM);
				}
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			OrderOfBattleFormationClassVM orderOfBattleFormationClassVM2 = list[k];
			orderOfBattleFormationClassVM2.SetWeightAdjustmentLock(isLocked: true);
			float num = OrderOfBattleUIHelper.GetCountOfRealUnitsInClass(orderOfBattleFormationClassVM2);
			float num2 = 0f;
			for (int l = 0; l < list.Count; l++)
			{
				OrderOfBattleFormationClassVM orderOfBattleFormationClassVM3 = list[l];
				if (orderOfBattleFormationClassVM3.Class == orderOfBattleFormationClassVM2.Class)
				{
					int countOfRealUnitsInClass = OrderOfBattleUIHelper.GetCountOfRealUnitsInClass(orderOfBattleFormationClassVM3);
					if (countOfRealUnitsInClass < 0 || countOfRealUnitsInClass > orderOfBattleFormationClassVM3.BelongedFormationItem.Formation.CountOfUnits)
					{
						orderOfBattleFormationClassVM3.SetWeightAdjustmentLock(isLocked: true);
						orderOfBattleFormationClassVM3.Weight = 0;
						orderOfBattleFormationClassVM3.SetWeightAdjustmentLock(isLocked: false);
						Debug.FailedAssert("Formation unit count is out of bounds! Skipping...", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "RefreshWeights", 731);
						Debug.Print("Formation unit count is out of bounds! Skipping...");
					}
					else
					{
						num2 += (float)countOfRealUnitsInClass;
					}
				}
			}
			orderOfBattleFormationClassVM2.Weight = TaleWorlds.Library.MathF.Round(num / num2 * 100f);
			orderOfBattleFormationClassVM2.IsLocked = !IsPlayerGeneral;
			orderOfBattleFormationClassVM2.SetWeightAdjustmentLock(isLocked: false);
		}
		for (FormationClass formationClass = FormationClass.Infantry; formationClass < FormationClass.NumberOfDefaultFormations; formationClass++)
		{
			List<OrderOfBattleFormationClassVM> list2 = new List<OrderOfBattleFormationClassVM>();
			for (int m = 0; m < list.Count; m++)
			{
				OrderOfBattleFormationClassVM orderOfBattleFormationClassVM4 = list[m];
				if (orderOfBattleFormationClassVM4.Class == formationClass)
				{
					list2.Add(orderOfBattleFormationClassVM4);
				}
			}
			if (list2.Count <= 1)
			{
				continue;
			}
			int num3 = 0;
			for (int n = 0; n < list2.Count; n++)
			{
				OrderOfBattleFormationClassVM orderOfBattleFormationClassVM5 = list2[n];
				if (orderOfBattleFormationClassVM5.Weight < 0 || orderOfBattleFormationClassVM5.Weight > 100)
				{
					orderOfBattleFormationClassVM5.SetWeightAdjustmentLock(isLocked: true);
					orderOfBattleFormationClassVM5.Weight = 0;
					orderOfBattleFormationClassVM5.SetWeightAdjustmentLock(isLocked: false);
					Debug.FailedAssert("Item weight is out of bounds! Skipping...", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "RefreshWeights", 772);
					Debug.Print("Item weight is out of bounds! Skipping...");
				}
				else
				{
					num3 += orderOfBattleFormationClassVM5.Weight;
				}
			}
			for (int num4 = TaleWorlds.Library.MathF.Abs(num3 - 100); num4 > 0; num4--)
			{
				bool flag = num3 < 100;
				OrderOfBattleFormationClassVM obj = (flag ? TaleWorlds.Core.Extensions.MinBy(list2, (OrderOfBattleFormationClassVM c) => c.Weight) : TaleWorlds.Core.Extensions.MaxBy(list2, (OrderOfBattleFormationClassVM c) => c.Weight));
				obj.SetWeightAdjustmentLock(isLocked: true);
				obj.Weight += (flag ? 1 : (-1));
				obj.SetWeightAdjustmentLock(isLocked: false);
			}
		}
		list.ForEach(delegate(OrderOfBattleFormationClassVM fc)
		{
			fc.UpdateWeightAdjustable();
		});
		list.ForEach(delegate(OrderOfBattleFormationClassVM fc)
		{
			fc.UpdateTroopCountText();
		});
	}

	public void OnAllFormationsAssignedSergeants(Dictionary<int, Agent> preAssignedCaptains)
	{
		foreach (KeyValuePair<int, Agent> preAssignedCaptain in preAssignedCaptains)
		{
			AssignCaptain(preAssignedCaptain.Value, _allFormations[preAssignedCaptain.Key]);
		}
	}

	private void OnClassSelectionToggled(OrderOfBattleFormationItemVM formationItem)
	{
		if (formationItem != null && formationItem.IsClassSelectionActive)
		{
			_lastEnabledClassSelection = formationItem;
		}
		else
		{
			_lastEnabledClassSelection = null;
		}
	}

	public bool IsAnyClassSelectionEnabled()
	{
		return _lastEnabledClassSelection != null;
	}

	public void ExecuteDisableAllClassSelections()
	{
		if (_lastEnabledClassSelection != null)
		{
			_lastEnabledClassSelection.IsClassSelectionActive = false;
			_lastEnabledClassSelection = null;
		}
	}

	private void SelectHeroItem(OrderOfBattleHeroItemVM heroItem)
	{
		if (!_selectedHeroes.Contains(heroItem))
		{
			heroItem.IsSelected = true;
			_selectedHeroes.Add(heroItem);
			UpdateHeroItemSelection();
			DeselectAllFormations();
		}
	}

	private void DeselectHeroItem(OrderOfBattleHeroItemVM heroItem)
	{
		heroItem.IsSelected = false;
		_selectedHeroes.Remove(heroItem);
		UpdateHeroItemSelection();
	}

	private void ToggleHeroItemSelection(OrderOfBattleHeroItemVM heroItem)
	{
		if (_selectedHeroes.Contains(heroItem))
		{
			DeselectHeroItem(heroItem);
		}
		else
		{
			SelectHeroItem(heroItem);
		}
		UpdateHeroItemSelection();
	}

	private void UpdateHeroItemSelection()
	{
		bool flag = _selectedHeroes.Count > 0;
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation.OnHeroSelectionUpdated(hasOwnHeroTroopInSelection: allFormation.HeroTroops.Any((OrderOfBattleHeroItemVM heroTroop) => _selectedHeroes.Contains(heroTroop)), selectedHeroCount: _selectedHeroes.Count);
		}
		IsPoolAcceptingCaptain = flag && _selectedHeroes.All((OrderOfBattleHeroItemVM hero) => hero.IsLeadingAFormation);
		IsPoolAcceptingHeroTroops = flag && !IsPoolAcceptingCaptain && _selectedHeroes.All((OrderOfBattleHeroItemVM hero) => hero.IsAssignedToAFormation);
		SelectedHeroCount = _selectedHeroes.Count;
		HasSelectedHeroes = flag;
		LastSelectedHeroItem = ((_selectedHeroes.Count > 0) ? _selectedHeroes[_selectedHeroes.Count - 1] : null);
	}

	private void OnHeroAssignmentBegin(OrderOfBattleHeroItemVM heroItem)
	{
		SelectHeroItem(heroItem);
		_selectedHeroes.ForEach(delegate(OrderOfBattleHeroItemVM hero)
		{
			hero.IsShown = false;
		});
	}

	private void OnHeroAssignmentEnd(OrderOfBattleHeroItemVM heroItem)
	{
		_selectedHeroes.ForEach(delegate(OrderOfBattleHeroItemVM hero)
		{
			hero.IsShown = true;
		});
		UpdateHeroItemSelection();
	}

	private void ClearAndSelectHeroItem(OrderOfBattleHeroItemVM heroItem)
	{
		ClearHeroItemSelection();
		SelectHeroItem(heroItem);
	}

	private void ClearHeroAssignment(OrderOfBattleHeroItemVM heroItem)
	{
		if (heroItem.IsLeadingAFormation)
		{
			heroItem.CurrentAssignedFormationItem.UnassignCaptain();
		}
		else if (heroItem.IsAssignedToAFormation)
		{
			heroItem.CurrentAssignedFormationItem.RemoveHeroTroop(heroItem);
		}
	}

	protected void AssignCaptain(Agent agent, OrderOfBattleFormationItemVM formationItem)
	{
		OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = _allHeroes.FirstOrDefault((OrderOfBattleHeroItemVM h) => h.Agent == agent);
		if (formationItem != null && orderOfBattleHeroItemVM != null && formationItem.Captain != orderOfBattleHeroItemVM)
		{
			if (formationItem.HasCaptain)
			{
				formationItem.Captain.IsSelected = false;
				formationItem.UnassignCaptain();
			}
			formationItem.Captain = orderOfBattleHeroItemVM;
		}
	}

	private void ClearHeroItemSelection()
	{
		_selectedHeroes.ForEach(delegate(OrderOfBattleHeroItemVM hero)
		{
			hero.IsSelected = false;
		});
		_selectedHeroes.Clear();
		UpdateHeroItemSelection();
	}

	public void ExecuteAcceptHeroes()
	{
		foreach (OrderOfBattleHeroItemVM selectedHero in _selectedHeroes)
		{
			ClearHeroAssignment(selectedHero);
			selectedHero.IsShown = true;
		}
		ClearHeroItemSelection();
	}

	public void ExecuteSelectAllHeroes()
	{
		ClearHeroItemSelection();
		foreach (OrderOfBattleHeroItemVM unassignedHero in UnassignedHeroes)
		{
			SelectHeroItem(unassignedHero);
		}
	}

	public void ExecuteClearHeroSelection()
	{
		ClearHeroItemSelection();
	}

	private void OnFormationAcceptCaptain(OrderOfBattleFormationItemVM formationItem)
	{
		if (_selectedHeroes.Count != 1)
		{
			_selectedHeroes.ForEach(delegate(OrderOfBattleHeroItemVM hero)
			{
				hero.IsShown = true;
			});
			ClearHeroItemSelection();
			return;
		}
		OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = _selectedHeroes[0];
		ClearHeroAssignment(orderOfBattleHeroItemVM);
		AssignCaptain(orderOfBattleHeroItemVM.Agent, formationItem);
		ClearHeroItemSelection();
		orderOfBattleHeroItemVM.IsShown = true;
		if (!IsPlayerGeneral)
		{
			_mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>().OnPlayerChoiceMade(formationItem.Formation.Index);
		}
		Game.Current?.EventManager.TriggerEvent(new OrderOfBattleHeroAssignedToFormationEvent(orderOfBattleHeroItemVM.Agent, formationItem.Formation));
	}

	private void OnFormationAcceptHeroTroops(OrderOfBattleFormationItemVM formationItem)
	{
		foreach (OrderOfBattleHeroItemVM selectedHero in _selectedHeroes)
		{
			ClearHeroAssignment(selectedHero);
			formationItem.AddHeroTroop(selectedHero);
			selectedHero.IsShown = true;
		}
		ClearHeroItemSelection();
	}

	private void OnHeroSelection(OrderOfBattleHeroItemVM heroSlotItem)
	{
		if (IsPlayerGeneral)
		{
			if (heroSlotItem.IsLeadingAFormation)
			{
				ClearAndSelectHeroItem(heroSlotItem);
			}
			else
			{
				ToggleHeroItemSelection(heroSlotItem);
			}
		}
		else
		{
			ToggleHeroItemSelection(heroSlotItem);
		}
	}

	private void OnFilterUseToggled(OrderOfBattleFormationItemVM formationItem)
	{
		foreach (OrderOfBattleFormationClassVM @class in formationItem.Classes)
		{
			if (@class.Class != FormationClass.NumberOfAllFormations)
			{
				DistributeTroops(@class);
			}
		}
	}

	public void OnDeploymentFinalized(bool playerDeployed)
	{
		if (playerDeployed)
		{
			_isSaving = true;
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => f.Captain.Agent == Agent.Main);
			if (orderOfBattleFormationItemVM != null)
			{
				AssignPlayerRoleInTeamMissionController missionBehavior = _mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>();
				missionBehavior.OnPlayerChoiceMade(orderOfBattleFormationItemVM.Formation.Index);
				missionBehavior.OnPlayerChoiceFinalized();
			}
			SaveConfiguration();
			_isSaving = false;
			if (_orderController != null)
			{
				_orderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
				_orderController.OnOrderIssued -= OnOrderIssued;
			}
		}
		IsEnabled = false;
	}

	private void OnHeroAssignedFormationChanged(OrderOfBattleHeroItemVM heroItem)
	{
		if (heroItem.IsAssignedToAFormation)
		{
			UnassignedHeroes.Remove(UnassignedHeroes.FirstOrDefault((OrderOfBattleHeroItemVM h) => h.Agent == heroItem.Agent));
		}
		else if (IsPlayerGeneral || heroItem.Agent.IsMainAgent)
		{
			UnassignedHeroes.Insert(0, heroItem);
		}
		_isTroopCountsDirty = true;
	}

	private bool CanAdjustWeight(OrderOfBattleFormationClassVM formationClass)
	{
		if (_isInitialized)
		{
			return OrderOfBattleUIHelper.GetMatchingClasses(_allFormations, formationClass).Count > 1;
		}
		return false;
	}

	private void OnWeightAdjusted(OrderOfBattleFormationClassVM formationClass)
	{
		if (_isInitialized)
		{
			DistributeWeights(formationClass);
			DistributeTroops(formationClass);
			Game.Current.EventManager.TriggerEvent(new OrderOfBattleFormationWeightChangedEvent(formationClass.BelongedFormationItem?.Formation));
		}
	}

	private void DistributeTroops(OrderOfBattleFormationClassVM formationClass)
	{
		List<(Formation, int, TroopTraitsMask, List<Agent>)> massTransferDataForFormation = GetMassTransferDataForFormation(formationClass);
		if (massTransferDataForFormation.Count > 0)
		{
			_orderController.RearrangeFormationsAccordingToFilters(_mission.PlayerTeam, massTransferDataForFormation);
			RefreshFormationsWithClass(formationClass.Class);
		}
	}

	private void DistributeWeights(OrderOfBattleFormationClassVM formationClass)
	{
		List<OrderOfBattleFormationClassVM> matchingClasses = OrderOfBattleUIHelper.GetMatchingClasses(_allFormations, formationClass);
		List<OrderOfBattleFormationClassVM> matchingClasses2 = OrderOfBattleUIHelper.GetMatchingClasses(_allFormations, formationClass, (OrderOfBattleFormationClassVM fc) => !fc.IsLocked);
		if (matchingClasses2.Count == 1)
		{
			formationClass.SetWeightAdjustmentLock(isLocked: true);
			formationClass.Weight = formationClass.PreviousWeight;
			formationClass.SetWeightAdjustmentLock(isLocked: false);
			return;
		}
		int num = OrderOfBattleUIHelper.GetMatchingClasses(_allFormations, formationClass, (OrderOfBattleFormationClassVM fc) => fc.IsLocked).Sum((OrderOfBattleFormationClassVM fc) => fc.Weight);
		int adjustableWeight = 100 - num;
		if (formationClass.Weight > adjustableWeight)
		{
			formationClass.SetWeightAdjustmentLock(isLocked: true);
			formationClass.Weight = adjustableWeight;
			formationClass.SetWeightAdjustmentLock(isLocked: false);
			matchingClasses2.Remove(formationClass);
			matchingClasses2.ForEach(delegate(OrderOfBattleFormationClassVM c)
			{
				c.SetWeightAdjustmentLock(isLocked: true);
				c.Weight = 0;
				c.SetWeightAdjustmentLock(isLocked: false);
			});
			return;
		}
		matchingClasses2.Remove(formationClass);
		int changePerClass = TaleWorlds.Library.MathF.Round((float)(formationClass.PreviousWeight - formationClass.Weight) / (float)matchingClasses2.Count);
		matchingClasses2.ForEach(delegate(OrderOfBattleFormationClassVM formation)
		{
			formation.SetWeightAdjustmentLock(isLocked: true);
		});
		if (changePerClass != 0)
		{
			matchingClasses2.ForEach(delegate(OrderOfBattleFormationClassVM formation)
			{
				int num4 = MBMath.ClampInt(changePerClass, -formation.Weight, adjustableWeight - formation.Weight);
				formation.Weight += num4;
			});
		}
		int num2 = matchingClasses.Sum((OrderOfBattleFormationClassVM c) => c.Weight);
		while (matchingClasses2.Count > 0 && num2 != 100)
		{
			int num3 = num2;
			if (num2 > 100)
			{
				OrderOfBattleFormationClassVM formationClassWithExtremumWeight = OrderOfBattleUIHelper.GetFormationClassWithExtremumWeight(matchingClasses2, isMinimum: false);
				if (formationClassWithExtremumWeight != null)
				{
					formationClassWithExtremumWeight.Weight--;
					num2--;
				}
			}
			else if (num2 < 100)
			{
				OrderOfBattleFormationClassVM formationClassWithExtremumWeight2 = OrderOfBattleUIHelper.GetFormationClassWithExtremumWeight(matchingClasses2, isMinimum: true);
				if (formationClassWithExtremumWeight2 != null)
				{
					formationClassWithExtremumWeight2.Weight++;
					num2++;
				}
			}
			if (num3 == num2)
			{
				Debug.FailedAssert("Failed to sum up all weights to 100", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "DistributeWeights", 1191);
				break;
			}
		}
		matchingClasses2.ForEach(delegate(OrderOfBattleFormationClassVM formation)
		{
			formation.SetWeightAdjustmentLock(isLocked: false);
		});
	}

	private void DistributeAllTroops()
	{
		if (_mission.PlayerTeam == null)
		{
			Debug.FailedAssert("Player team should be initialized before distributing troops", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\OrderOfBattle\\OrderOfBattleVM.cs", "DistributeAllTroops", 1203);
			Debug.Print("Player team should be initialized before distributing troops");
			return;
		}
		List<(Formation, int, TroopTraitsMask, List<Agent>)> list = new List<(Formation, int, TroopTraitsMask, List<Agent>)>();
		List<FormationClass> list2 = new List<FormationClass>();
		for (int i = 0; i < _allFormations.Count; i++)
		{
			for (int j = 0; j < _allFormations[i].Classes.Count; j++)
			{
				OrderOfBattleFormationClassVM orderOfBattleFormationClassVM = _allFormations[i].Classes[j];
				if (!orderOfBattleFormationClassVM.IsUnset && !list2.Contains(orderOfBattleFormationClassVM.Class))
				{
					List<(Formation, int, TroopTraitsMask, List<Agent>)> massTransferDataForFormation = GetMassTransferDataForFormation(orderOfBattleFormationClassVM);
					list.AddRange(massTransferDataForFormation);
					list2.Add(orderOfBattleFormationClassVM.Class);
				}
			}
			if (list.Count > 0)
			{
				_orderController.RearrangeFormationsAccordingToFilters(_mission.PlayerTeam, list);
			}
			list.Clear();
			if (list2.Count == 4)
			{
				break;
			}
		}
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.OnSizeChanged();
		});
	}

	private List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> GetMassTransferDataForFormationClass(Formation targetFormation, FormationClass formationClass)
	{
		List<(Formation, int, TroopTraitsMask, List<Agent>)> list = new List<(Formation, int, TroopTraitsMask, List<Agent>)>();
		List<OrderOfBattleFormationItemVM> list2 = new List<OrderOfBattleFormationItemVM>();
		List<int> list3 = new List<int>();
		int num = 0;
		for (int i = 0; i < _allFormations.Count; i++)
		{
			int totalCountOfUnitsInClass = OrderOfBattleUIHelper.GetTotalCountOfUnitsInClass(_allFormations[i].Formation, formationClass);
			if (totalCountOfUnitsInClass > 0 || _allFormations[i].Formation == targetFormation)
			{
				list2.Add(_allFormations[i]);
				list3.Add(totalCountOfUnitsInClass);
				num += totalCountOfUnitsInClass;
			}
		}
		if (list2.Count == 1)
		{
			return list;
		}
		if (num > 0)
		{
			List<int> list4 = new List<int>();
			for (int j = 0; j < list2.Count; j++)
			{
				int item = ((list2[j].Formation == targetFormation) ? num : 0);
				list4.Add(item);
			}
			int num2 = 0;
			while (list4.Count > 0 && (num2 = list4.Sum()) != num)
			{
				int num3 = num2 - num;
				list4[(num3 > 0) ? list4.IndexOfMax((int c) => c) : list4.IndexOfMin((int c) => c)] -= Math.Sign(num3);
			}
			for (int num4 = 0; num4 < list4.Count; num4++)
			{
				OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = list2[num4];
				TroopTraitsMask filter = TroopFilteringUtilities.GetFilter(formationClass);
				filter |= TroopFilteringUtilities.GetFilter((from f in orderOfBattleFormationItemVM.FilterItems
					where f.IsActive
					select f.FilterType).ToArray());
				if (filter != TroopTraitsMask.None)
				{
					(Formation, int, TroopTraitsMask, List<Agent>) item2 = OrderOfBattleUIHelper.CreateMassTransferData(orderOfBattleFormationItemVM, formationClass, filter, list4[num4]);
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> GetMassTransferDataForFormation(OrderOfBattleFormationClassVM formationClass)
	{
		List<(Formation, int, TroopTraitsMask, List<Agent>)> list = new List<(Formation, int, TroopTraitsMask, List<Agent>)>();
		List<OrderOfBattleFormationClassVM> allFormationClassesWith = GetAllFormationClassesWith(formationClass.Class);
		if (allFormationClassesWith.Count == 1)
		{
			return list;
		}
		int num = allFormationClassesWith.Sum((OrderOfBattleFormationClassVM c) => OrderOfBattleUIHelper.GetCountOfRealUnitsInClass(c));
		if (num > 0)
		{
			List<int> list2 = new List<int>();
			for (int num2 = 0; num2 < allFormationClassesWith.Count; num2++)
			{
				int item = TaleWorlds.Library.MathF.Ceiling((float)allFormationClassesWith[num2].Weight / 100f * (float)num);
				list2.Add(item);
			}
			int num3 = 0;
			while (list2.Count > 0 && (num3 = list2.Sum()) != num)
			{
				int num4 = num3 - num;
				list2[(num4 > 0) ? list2.IndexOfMax((int c) => c) : list2.IndexOfMin((int c) => c)] -= Math.Sign(num4);
			}
			for (int num5 = 0; num5 < list2.Count; num5++)
			{
				OrderOfBattleFormationItemVM belongedFormationItem = allFormationClassesWith[num5].BelongedFormationItem;
				TroopTraitsMask filter = TroopFilteringUtilities.GetFilter((from c in belongedFormationItem.Classes
					where !c.IsUnset
					select c.Class).ToArray());
				filter |= TroopFilteringUtilities.GetFilter((from f in belongedFormationItem.FilterItems
					where f.IsActive
					select f.FilterType).ToArray());
				if (filter != TroopTraitsMask.None)
				{
					(Formation, int, TroopTraitsMask, List<Agent>) item2 = OrderOfBattleUIHelper.CreateMassTransferData(allFormationClassesWith[num5], allFormationClassesWith[num5].Class, filter, list2[num5]);
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private List<OrderOfBattleFormationClassVM> GetAllFormationClassesWith(FormationClass formationClass)
	{
		List<OrderOfBattleFormationClassVM> list = new List<OrderOfBattleFormationClassVM>();
		if (formationClass >= FormationClass.NumberOfDefaultFormations)
		{
			return list;
		}
		for (int i = 0; i < _allFormations.Count; i++)
		{
			for (int j = 0; j < _allFormations[i].Classes.Count; j++)
			{
				if (_allFormations[i].Classes[j].Class == formationClass)
				{
					list.Add(_allFormations[i].Classes[j]);
				}
			}
		}
		return list;
	}

	private void RefreshFormationsWithClass(FormationClass formationClass)
	{
		for (int i = 0; i < _allFormations.Count; i++)
		{
			for (int j = 0; j < _allFormations[i].Classes.Count; j++)
			{
				if (_allFormations[i].Classes[j].Class == formationClass)
				{
					_allFormations[i].OnSizeChanged();
					break;
				}
			}
		}
	}

	private List<Agent> GetLockedAgents()
	{
		List<Agent> list = new List<Agent>();
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			if (allFormation.Captain.Agent != null)
			{
				list.Add(allFormation.Captain.Agent);
			}
			foreach (OrderOfBattleHeroItemVM heroTroop in allFormation.HeroTroops)
			{
				list.Add(heroTroop.Agent);
			}
		}
		return list;
	}

	private void OnFormationClassChanged(OrderOfBattleFormationClassVM formationClassItem, FormationClass newFormationClass)
	{
		if (!_isInitialized)
		{
			return;
		}
		List<OrderOfBattleFormationClassVM> previousFormationClasses = new List<OrderOfBattleFormationClassVM>();
		List<OrderOfBattleFormationClassVM> newFormationClasses = new List<OrderOfBattleFormationClassVM>();
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM formation)
		{
			previousFormationClasses.AddRange(from fc in formation.Classes.ToList()
				where fc.Class == formationClassItem.Class
				select fc);
			newFormationClasses.AddRange(from fc in formation.Classes.ToList()
				where fc.Class == newFormationClass
				select fc);
		});
		if (newFormationClasses.Count > 0)
		{
			formationClassItem.Weight = 0;
		}
		else
		{
			TransferAllAvailableTroopsToFormation(formationClassItem.BelongedFormationItem, newFormationClass);
			formationClassItem.SetWeightAdjustmentLock(isLocked: true);
			formationClassItem.Weight = 100;
			formationClassItem.SetWeightAdjustmentLock(isLocked: false);
		}
		newFormationClasses.Add(formationClassItem);
		previousFormationClasses.ForEach(delegate(OrderOfBattleFormationClassVM fc)
		{
			fc.IsAdjustable = formationClassItem.Class != FormationClass.NumberOfAllFormations && previousFormationClasses.Count > 2;
		});
		newFormationClasses.ForEach(delegate(OrderOfBattleFormationClassVM fc)
		{
			fc.IsAdjustable = newFormationClass != FormationClass.NumberOfAllFormations && newFormationClasses.Count > 1;
		});
		List<OrderOfBattleFormationClassVM> allClasses = new List<OrderOfBattleFormationClassVM>();
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM formation)
		{
			allClasses.AddRange(formation.Classes.Where((OrderOfBattleFormationClassVM fc) => fc.Class != FormationClass.NumberOfAllFormations));
		});
		if (newFormationClass != FormationClass.NumberOfAllFormations || allClasses.Contains(formationClassItem))
		{
			allClasses.Remove(formationClassItem);
			allClasses.Add(new OrderOfBattleFormationClassVM(formationClassItem.BelongedFormationItem, newFormationClass));
		}
		bool oldValue = _orderController.BackupAndDisableGesturesEnabled();
		foreach (OrderOfBattleFormationItemVM item in _allFormations.Where((OrderOfBattleFormationItemVM f) => f.Classes.All((OrderOfBattleFormationClassVM c) => c.Class != newFormationClass)))
		{
			(int, bool, bool) relevantTroopTransferParameters = OrderOfBattleUIHelper.GetRelevantTroopTransferParameters(item.Classes.FirstOrDefault((OrderOfBattleFormationClassVM c) => c.Class == newFormationClass));
			if (relevantTroopTransferParameters.Item1 <= 0)
			{
				continue;
			}
			_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
			{
				if (_orderController.SelectedFormations.Contains(f.Formation))
				{
					_orderController.DeselectFormation(f.Formation);
				}
			});
			_orderController.SelectFormation(item.Formation);
			_orderController.TransferUnitWithPriorityFunction(formationClassItem.BelongedFormationItem.Formation, relevantTroopTransferParameters.Item1, hasShield: false, hasSpear: false, hasThrown: false, isHeavy: false, relevantTroopTransferParameters.Item2, relevantTroopTransferParameters.Item3, excludeBannerman: true, GetLockedAgents());
			item.OnSizeChanged();
			formationClassItem.BelongedFormationItem.OnSizeChanged();
		}
		_isTroopCountsDirty = true;
		_isHeroSelectionDirty = true;
		_isMissingFormationsDirty = true;
		_orderController.RestoreGesturesEnabled(oldValue);
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.UpdateAdjustable();
		});
		Game.Current.EventManager.TriggerEvent(new OrderOfBattleFormationClassChangedEvent(formationClassItem.BelongedFormationItem?.Formation));
	}

	private void TransferAllAvailableTroopsToFormation(OrderOfBattleFormationItemVM formation, FormationClass formationClass)
	{
		List<(Formation, int, TroopTraitsMask, List<Agent>)> massTransferDataForFormationClass = GetMassTransferDataForFormationClass(formation.Formation, formationClass);
		if (massTransferDataForFormationClass.Count > 0)
		{
			_orderController.RearrangeFormationsAccordingToFilters(_mission.PlayerTeam, massTransferDataForFormationClass);
			RefreshFormationsWithClass(formationClass);
		}
	}

	private void RefreshMissingFormations()
	{
		if (!IsPlayerGeneral)
		{
			return;
		}
		List<OrderOfBattleFormationClassVM> allClasses = new List<OrderOfBattleFormationClassVM>();
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM formation)
		{
			allClasses.AddRange(formation.Classes.Where((OrderOfBattleFormationClassVM fc) => fc.Class != FormationClass.NumberOfAllFormations));
		});
		bool flag = false;
		foreach (FormationClass availableTroopType in _availableTroopTypes)
		{
			if (!allClasses.All((OrderOfBattleFormationClassVM c) => c.Class != availableTroopType))
			{
				continue;
			}
			if (Mission.Current.IsSiegeBattle)
			{
				if (availableTroopType != FormationClass.HorseArcher && availableTroopType != FormationClass.Cavalry)
				{
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				MissingFormationsHint.HintText.SetTextVariable("FORMATION_CLASS", availableTroopType.GetLocalizedName());
				CanStartMission = false;
				break;
			}
		}
		CanStartMission = !flag;
	}

	private OrderOfBattleFormationItemVM GetFormationItemAtIndex(int index)
	{
		if (index < TotalFormationCount / 2)
		{
			return FormationsFirstHalf.ElementAt(index);
		}
		if (index < TotalFormationCount)
		{
			return FormationsSecondHalf.ElementAt(index - TotalFormationCount / 2);
		}
		return null;
	}

	private IEnumerable<OrderOfBattleFormationItemVM> GetFormationItemsWithCondition(Func<OrderOfBattleFormationItemVM, bool> condition)
	{
		return _allFormations.Where(condition);
	}

	private void OnSelectedFormationsChanged()
	{
		if (_isInitialized)
		{
			for (int i = 0; i < _allFormations.Count; i++)
			{
				OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
				orderOfBattleFormationItemVM.IsSelected = _orderController.IsFormationListening(orderOfBattleFormationItemVM.Formation);
			}
		}
	}

	private void SelectFormationItem(OrderOfBattleFormationItemVM formationItem)
	{
		formationItem.IsSelected = true;
		_selectFormationAtIndex(formationItem.Formation.Index);
		ExecuteClearHeroSelection();
	}

	private void DeselectFormationItem(OrderOfBattleFormationItemVM formationItem)
	{
		Formation formation = formationItem.Formation;
		if (formation != null && formation.Index >= 0)
		{
			Mission.Current.PlayerTeam.PlayerOrderController.DeselectFormation(formationItem.Formation);
			_deselectFormationAtIndex?.Invoke(formationItem.Formation.Index);
		}
	}

	public void SelectFormationItemAtIndex(int index)
	{
		_allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => f.Formation.Index == index).IsSelected = true;
		_selectFormationAtIndex(index);
	}

	public void FocusFormationItemAtIndex(int index)
	{
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.IsBeingFocused = false;
		});
		_allFormations.FirstOrDefault((OrderOfBattleFormationItemVM f) => f.Formation.Index == index).IsBeingFocused = true;
	}

	public void DeselectAllFormations()
	{
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation.IsSelected = false;
		}
		_clearFormationSelection?.Invoke();
	}

	public void OnUnitDeployed()
	{
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f?.MakeMarkerWorldPositionDirty();
		});
	}

	public bool OnEscape()
	{
		if (_allFormations.Any((OrderOfBattleFormationItemVM f) => f.IsSelected))
		{
			DeselectAllFormations();
			return true;
		}
		return false;
	}

	private int GetTroopCountWithFilter(DeploymentFormationClass orderOfBattleFormationClass, FormationFilterType filterType)
	{
		int num = 0;
		List<FormationClass> formationClasses = orderOfBattleFormationClass.GetFormationClasses();
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			List<FormationClass> second = (from c in allFormation.Classes
				select c.Class into c
				where c != FormationClass.NumberOfAllFormations
				select c).ToList();
			if (!formationClasses.Intersect(second).Any())
			{
				continue;
			}
			switch (filterType)
			{
			case FormationFilterType.Shield:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.HasShieldCached);
				break;
			case FormationFilterType.Spear:
				num += allFormation.Formation.GetCountOfUnitsWithCondition((Agent a) => a.HasSpearCached);
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

	protected void ClearFormationItem(OrderOfBattleFormationItemVM formationItem)
	{
		formationItem.FormationClassSelector.SelectedIndex = 0;
		formationItem.UnassignCaptain();
		foreach (OrderOfBattleFormationClassVM @class in formationItem.Classes)
		{
			@class.IsLocked = false;
			@class.Weight = 0;
			@class.Class = FormationClass.NumberOfAllFormations;
		}
	}

	private int GetVisibleTotalTroopCountOfType(FormationClass formationClass)
	{
		return _visibleTroopTypeCountLookup[formationClass];
	}

	private void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM x)
		{
			x.MakeMarkerWorldPositionDirty();
		});
	}

	private void OnHeroesChanged()
	{
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.OnSizeChanged();
			f.UpdateAdjustable();
		});
		RefreshWeights();
	}

	public void ExecuteAutoDeploy()
	{
		if (IsPlayerGeneral)
		{
			_onAutoDeploy();
			AfterAutoDeploy();
		}
	}

	private void AfterAutoDeploy()
	{
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			allFormation.RefreshFormation(allFormation.Formation);
		}
		ClearHeroItemSelection();
		ClearAllHeroAssignments();
		RefreshWeights();
		OnUnitDeployed();
		_allFormations.ForEach(delegate(OrderOfBattleFormationItemVM f)
		{
			f.UpdateAdjustable();
		});
		_isMissingFormationsDirty = true;
		_mission.PlayerTeam.TriggerOnFormationsChangedInDeployment();
	}

	public void ExecuteBeginMission()
	{
		CurrentConfiguration?.Clear();
		foreach (OrderOfBattleFormationItemVM allFormation in _allFormations)
		{
			if (allFormation.Formation.CountOfUnits > 0)
			{
				CurrentConfiguration?.Add(new MissionOrderVM.FormationConfiguration(allFormation.Formation.Index, (from f in allFormation.FilterItems
					where f.IsActive
					select f.FilterType).ToList()));
			}
			else
			{
				CurrentConfiguration?.Add(new MissionOrderVM.FormationConfiguration(allFormation.Formation.Index, new List<FormationFilterType>()));
			}
		}
		if (_bannerBearerLogic != null)
		{
			_bannerBearerLogic.OnBannerBearersUpdated -= OnBannerBearersUpdated;
			_bannerBearerLogic.OnBannerBearerAgentUpdated -= OnBannerAgentUpdated;
		}
		_onBeginMission?.Invoke();
		MBInformationManager.HideInformations();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (!(obj.NewNotificationElementID != _latestTutorialElementID))
		{
			return;
		}
		if (_latestTutorialElementID != null)
		{
			if (_isAssignCaptainHighlightApplied)
			{
				SetHighlightEmptyCaptainFormations(state: false);
				SetHighlightMainAgentPortait(state: false);
				_isAssignCaptainHighlightApplied = false;
			}
			if (_isCreateFormationHighlightApplied)
			{
				SetHighlightFormationTypeSelection(state: false);
				SetHighlightFormationWeights(state: false);
				_isCreateFormationHighlightApplied = false;
			}
		}
		_latestTutorialElementID = obj.NewNotificationElementID;
		if (_latestTutorialElementID != null)
		{
			if (_latestTutorialElementID == "AssignCaptain" && !_isAssignCaptainHighlightApplied)
			{
				SetHighlightEmptyCaptainFormations(state: true);
				SetHighlightMainAgentPortait(state: true);
				_isAssignCaptainHighlightApplied = true;
			}
			if (_latestTutorialElementID == "CreateFormation" && !_isCreateFormationHighlightApplied)
			{
				SetHighlightFormationTypeSelection(state: true);
				SetHighlightFormationWeights(state: true);
				_isCreateFormationHighlightApplied = true;
			}
		}
	}

	private void SetHighlightMainAgentPortait(bool state)
	{
		for (int i = 0; i < _allHeroes.Count; i++)
		{
			OrderOfBattleHeroItemVM orderOfBattleHeroItemVM = _allHeroes[i];
			if (orderOfBattleHeroItemVM.Agent.IsMainAgent)
			{
				orderOfBattleHeroItemVM.IsHighlightActive = state;
				break;
			}
		}
	}

	private void SetHighlightEmptyCaptainFormations(bool state)
	{
		for (int i = 0; i < _allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
			if (!state || (!orderOfBattleFormationItemVM.HasCaptain && orderOfBattleFormationItemVM.HasFormation))
			{
				orderOfBattleFormationItemVM.IsCaptainSlotHighlightActive = state;
			}
		}
	}

	private void SetHighlightFormationTypeSelection(bool state)
	{
		for (int i = 0; i < _allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
			if (!state || orderOfBattleFormationItemVM.IsAdjustable)
			{
				_allFormations[i].IsTypeSelectionHighlightActive = state;
			}
		}
	}

	private void SetHighlightFormationWeights(bool state)
	{
		for (int i = 0; i < _allFormations.Count; i++)
		{
			OrderOfBattleFormationItemVM orderOfBattleFormationItemVM = _allFormations[i];
			for (int j = 0; j < orderOfBattleFormationItemVM.Classes.Count; j++)
			{
				orderOfBattleFormationItemVM.Classes[j].IsWeightHighlightActive = state;
			}
		}
	}
}
