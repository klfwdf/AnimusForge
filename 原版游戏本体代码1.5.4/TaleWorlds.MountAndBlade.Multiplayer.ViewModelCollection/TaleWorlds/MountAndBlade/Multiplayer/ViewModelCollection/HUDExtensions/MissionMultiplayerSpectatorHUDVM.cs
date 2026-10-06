using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MissionMultiplayerSpectatorHUDVM : ViewModel
{
	private const string KillStatId = "kill";

	private const string DeathStatId = "death";

	private const string AssistStatId = "assist";

	private const string GoldStatId = "gold";

	private readonly Mission _mission;

	private readonly bool _isTeamsEnabled;

	private readonly bool _isFlagDominationMode;

	private Agent _spectatedAgent;

	private MissionPeer _spectatedPeer;

	private int _cachedKillCount = -1;

	private int _cachedDeathCount = -1;

	private int _cachedAssistCount = -1;

	private int _cachedGold = -1;

	private string _cachedClanName;

	private string _cachedLastKillVictimName;

	private string _cachedMostUsedWeaponName;

	private readonly ItemObject[] _cachedWeaponItems = new ItemObject[4];

	private int _cachedPerkSelectedTroopIndex = -1;

	private MissionPeer _cachedPerkPeer;

	private readonly MissionMultiplayerGameModeBaseClient _gameModeClient;

	private readonly bool _isGameModeUsingGold;

	private MPOverlayStatVM _killCountStat;

	private MPOverlayStatVM _deathCountStat;

	private MPOverlayStatVM _assistCountStat;

	private MPOverlayStatVM _goldStat;

	private string _spectatedPlayerName;

	private string _takeControlText;

	private int _spectatedPlayerNeutrality = -1;

	private bool _isSpectatingPlayer;

	private bool _canTakeControlOfSpectatedAgent;

	private bool _agentHasMount;

	private bool _agentHasShield;

	private bool _showAgentHealth;

	private Color _teamColor = Color.White;

	private InputKeyItemVM _cyclePreviousKey;

	private InputKeyItemVM _cycleNextKey;

	private bool _agentHasRangedWeapon;

	private bool _agentHasCompassElement;

	private BannerImageIdentifierVM _spectatedPlayerSigil;

	private bool _agentHasSigil;

	private bool _showBothTeamsData;

	private bool _isSpectating;

	private bool _showAgentStats;

	private MBBindingList<MPOverlayStatVM> _spectatorStats;

	private string _spectatorClanText;

	private string _spectatorLastKillText;

	private string _spectatorMostUsedWeaponText;

	private bool _showAgentWeapons;

	private MBBindingList<SpectatorWeaponSlotVM> _spectatedPlayerWeapons;

	private MBBindingList<MPPerkVM> _spectatedPlayerPerks;

	private bool _showAgentPerks;

	private float _spectatedPlayerHealthLimit;

	private float _spectatedPlayerCurrentHealth;

	private float _spectatedPlayerMountCurrentHealth;

	private float _spectatedPlayerMountHealthLimit;

	private float _spectatedPlayerShieldCurrentHealth;

	private float _spectatedPlayerShieldHealthLimit;

	private int _spectatedPlayerAmmoAmount;

	private MPTeammateCompassTargetVM _compassElement;

	[DataSourceProperty]
	public InputKeyItemVM CyclePreviousKey
	{
		get
		{
			return _cyclePreviousKey;
		}
		set
		{
			if (value != _cyclePreviousKey)
			{
				_cyclePreviousKey = value;
				OnPropertyChangedWithValue(value, "CyclePreviousKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CycleNextKey
	{
		get
		{
			return _cycleNextKey;
		}
		set
		{
			if (value != _cycleNextKey)
			{
				_cycleNextKey = value;
				OnPropertyChangedWithValue(value, "CycleNextKey");
			}
		}
	}

	[DataSourceProperty]
	public Color TeamColor
	{
		get
		{
			return _teamColor;
		}
		set
		{
			if (value != _teamColor)
			{
				_teamColor = value;
				OnPropertyChangedWithValue(value, "TeamColor");
			}
		}
	}

	[DataSourceProperty]
	public int SpectatedPlayerNeutrality
	{
		get
		{
			return _spectatedPlayerNeutrality;
		}
		set
		{
			if (value != _spectatedPlayerNeutrality)
			{
				_spectatedPlayerNeutrality = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerNeutrality");
				IsSpectatingAgent = value >= 0;
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM CompassElement
	{
		get
		{
			return _compassElement;
		}
		set
		{
			if (value != _compassElement)
			{
				_compassElement = value;
				OnPropertyChangedWithValue(value, "CompassElement");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM SpectatedPlayerSigil
	{
		get
		{
			return _spectatedPlayerSigil;
		}
		set
		{
			if (value != _spectatedPlayerSigil)
			{
				_spectatedPlayerSigil = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerSigil");
			}
		}
	}

	[DataSourceProperty]
	public bool AgentHasSigil
	{
		get
		{
			return _agentHasSigil;
		}
		set
		{
			if (value != _agentHasSigil)
			{
				_agentHasSigil = value;
				OnPropertyChangedWithValue(value, "AgentHasSigil");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpectatingAgent
	{
		get
		{
			return _isSpectatingPlayer;
		}
		set
		{
			if (value != _isSpectatingPlayer)
			{
				_isSpectatingPlayer = value;
				OnPropertyChangedWithValue(value, "IsSpectatingAgent");
			}
		}
	}

	[DataSourceProperty]
	public bool AgentHasCompassElement
	{
		get
		{
			return _agentHasCompassElement;
		}
		set
		{
			if (value != _agentHasCompassElement)
			{
				_agentHasCompassElement = value;
				OnPropertyChangedWithValue(value, "AgentHasCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public bool AgentHasMount
	{
		get
		{
			return _agentHasMount;
		}
		set
		{
			if (value != _agentHasMount)
			{
				_agentHasMount = value;
				OnPropertyChangedWithValue(value, "AgentHasMount");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAgentHealth
	{
		get
		{
			return _showAgentHealth;
		}
		set
		{
			if (value != _showAgentHealth)
			{
				_showAgentHealth = value;
				OnPropertyChangedWithValue(value, "ShowAgentHealth");
			}
		}
	}

	[DataSourceProperty]
	public bool AgentHasRangedWeapon
	{
		get
		{
			return _agentHasRangedWeapon;
		}
		set
		{
			if (value != _agentHasRangedWeapon)
			{
				_agentHasRangedWeapon = value;
				OnPropertyChangedWithValue(value, "AgentHasRangedWeapon");
			}
		}
	}

	[DataSourceProperty]
	public bool AgentHasShield
	{
		get
		{
			return _agentHasShield;
		}
		set
		{
			if (value != _agentHasShield)
			{
				_agentHasShield = value;
				OnPropertyChangedWithValue(value, "AgentHasShield");
			}
		}
	}

	[DataSourceProperty]
	public bool CanTakeControlOfSpectatedAgent
	{
		get
		{
			return _canTakeControlOfSpectatedAgent;
		}
		set
		{
			if (value != _canTakeControlOfSpectatedAgent)
			{
				_canTakeControlOfSpectatedAgent = value;
				OnPropertyChangedWithValue(value, "CanTakeControlOfSpectatedAgent");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatedPlayerName
	{
		get
		{
			return _spectatedPlayerName;
		}
		set
		{
			if (value != _spectatedPlayerName)
			{
				_spectatedPlayerName = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerName");
			}
		}
	}

	[DataSourceProperty]
	public string TakeControlText
	{
		get
		{
			return _takeControlText;
		}
		set
		{
			if (value != _takeControlText)
			{
				_takeControlText = value;
				OnPropertyChangedWithValue(value, "TakeControlText");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerHealthLimit
	{
		get
		{
			return _spectatedPlayerHealthLimit;
		}
		set
		{
			if (value != _spectatedPlayerHealthLimit)
			{
				_spectatedPlayerHealthLimit = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerHealthLimit");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerCurrentHealth
	{
		get
		{
			return _spectatedPlayerCurrentHealth;
		}
		set
		{
			if (value != _spectatedPlayerCurrentHealth)
			{
				_spectatedPlayerCurrentHealth = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerCurrentHealth");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerMountCurrentHealth
	{
		get
		{
			return _spectatedPlayerMountCurrentHealth;
		}
		set
		{
			if (value != _spectatedPlayerMountCurrentHealth)
			{
				_spectatedPlayerMountCurrentHealth = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerMountCurrentHealth");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerMountHealthLimit
	{
		get
		{
			return _spectatedPlayerMountHealthLimit;
		}
		set
		{
			if (value != _spectatedPlayerMountHealthLimit)
			{
				_spectatedPlayerMountHealthLimit = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerMountHealthLimit");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerShieldCurrentHealth
	{
		get
		{
			return _spectatedPlayerShieldCurrentHealth;
		}
		set
		{
			if (value != _spectatedPlayerShieldCurrentHealth)
			{
				_spectatedPlayerShieldCurrentHealth = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerShieldCurrentHealth");
			}
		}
	}

	[DataSourceProperty]
	public float SpectatedPlayerShieldHealthLimit
	{
		get
		{
			return _spectatedPlayerShieldHealthLimit;
		}
		set
		{
			if (value != _spectatedPlayerShieldHealthLimit)
			{
				_spectatedPlayerShieldHealthLimit = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerShieldHealthLimit");
			}
		}
	}

	[DataSourceProperty]
	public int SpectatedPlayerAmmoAmount
	{
		get
		{
			return _spectatedPlayerAmmoAmount;
		}
		set
		{
			if (value != _spectatedPlayerAmmoAmount)
			{
				_spectatedPlayerAmmoAmount = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerAmmoAmount");
			}
		}
	}

	public bool ShowBothTeamsData
	{
		get
		{
			return _showBothTeamsData;
		}
		set
		{
			_showBothTeamsData = value;
		}
	}

	public bool IsSpectating
	{
		get
		{
			return _isSpectating;
		}
		set
		{
			_isSpectating = value;
		}
	}

	[DataSourceProperty]
	public bool ShowAgentStats
	{
		get
		{
			return _showAgentStats;
		}
		set
		{
			if (value != _showAgentStats)
			{
				_showAgentStats = value;
				OnPropertyChangedWithValue(value, "ShowAgentStats");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPOverlayStatVM> SpectatorStats
	{
		get
		{
			return _spectatorStats;
		}
		set
		{
			if (value != _spectatorStats)
			{
				_spectatorStats = value;
				OnPropertyChangedWithValue(value, "SpectatorStats");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatorClanText
	{
		get
		{
			return _spectatorClanText;
		}
		set
		{
			if (value != _spectatorClanText)
			{
				_spectatorClanText = value;
				OnPropertyChangedWithValue(value, "SpectatorClanText");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatorLastKillText
	{
		get
		{
			return _spectatorLastKillText;
		}
		set
		{
			if (value != _spectatorLastKillText)
			{
				_spectatorLastKillText = value;
				OnPropertyChangedWithValue(value, "SpectatorLastKillText");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatorMostUsedWeaponText
	{
		get
		{
			return _spectatorMostUsedWeaponText;
		}
		set
		{
			if (value != _spectatorMostUsedWeaponText)
			{
				_spectatorMostUsedWeaponText = value;
				OnPropertyChangedWithValue(value, "SpectatorMostUsedWeaponText");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAgentWeapons
	{
		get
		{
			return _showAgentWeapons;
		}
		set
		{
			if (value != _showAgentWeapons)
			{
				_showAgentWeapons = value;
				OnPropertyChangedWithValue(value, "ShowAgentWeapons");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SpectatorWeaponSlotVM> SpectatedPlayerWeapons
	{
		get
		{
			return _spectatedPlayerWeapons;
		}
		set
		{
			if (value != _spectatedPlayerWeapons)
			{
				_spectatedPlayerWeapons = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerWeapons");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAgentPerks
	{
		get
		{
			return _showAgentPerks;
		}
		set
		{
			if (value != _showAgentPerks)
			{
				_showAgentPerks = value;
				OnPropertyChangedWithValue(value, "ShowAgentPerks");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPerkVM> SpectatedPlayerPerks
	{
		get
		{
			return _spectatedPlayerPerks;
		}
		set
		{
			if (value != _spectatedPlayerPerks)
			{
				_spectatedPlayerPerks = value;
				OnPropertyChangedWithValue(value, "SpectatedPlayerPerks");
			}
		}
	}

	public event Action<int> OnCycleTargetRequested;

	private void RefreshCycleTargetKeys()
	{
		GameKeyContext category = HotKeyManager.GetCategory("MultiplayerHotkeyCategory");
		HotKey hotKey = category?.GetHotKey("CycleSpectatorTargetPrevious");
		if (hotKey != null)
		{
			CyclePreviousKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: false);
		}
		HotKey hotKey2 = category?.GetHotKey("CycleSpectatorTargetNext");
		if (hotKey2 != null)
		{
			CycleNextKey = InputKeyItemVM.CreateFromHotKey(hotKey2, isConsoleOnly: false);
		}
	}

	public void ExecuteCyclePreviousTarget()
	{
		this.OnCycleTargetRequested?.Invoke(-1);
	}

	public void ExecuteCycleNextTarget()
	{
		this.OnCycleTargetRequested?.Invoke(1);
	}

	public MissionMultiplayerSpectatorHUDVM(Mission mission)
	{
		_mission = mission;
		MissionLobbyComponent missionBehavior = mission.GetMissionBehavior<MissionLobbyComponent>();
		_isTeamsEnabled = missionBehavior == null || missionBehavior.MissionType != MultiplayerGameType.Duel;
		_isFlagDominationMode = Mission.Current.HasMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
		_gameModeClient = mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_isGameModeUsingGold = _gameModeClient != null && _gameModeClient.IsGameModeUsingGold;
		SpectatedPlayerWeapons = new MBBindingList<SpectatorWeaponSlotVM>();
		SpectatedPlayerPerks = new MBBindingList<MPPerkVM>();
		SpectatorStats = new MBBindingList<MPOverlayStatVM>();
		BuildStats();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
		GameTexts.SetVariable("USE_KEY", keyHyperlinkText);
		TakeControlText = GameTexts.FindText("str_sergeant_battle_press_action_to_control_bot_2").ToString();
		RefreshCycleTargetKeys();
		RefreshStatNames();
	}

	private void BuildStats()
	{
		_killCountStat = AddStat("kill");
		_deathCountStat = AddStat("death");
		_assistCountStat = AddStat("assist");
		if (_isGameModeUsingGold)
		{
			_goldStat = AddStat("gold");
		}
	}

	private MPOverlayStatVM AddStat(string statId)
	{
		MPOverlayStatVM mPOverlayStatVM = new MPOverlayStatVM(statId, GetStatName(statId), string.Empty);
		SpectatorStats.Add(mPOverlayStatVM);
		return mPOverlayStatVM;
	}

	private void RefreshEquippedWeaponSlot()
	{
		if (SpectatedPlayerWeapons.Count != 0)
		{
			EquipmentIndex equipmentIndex = EquipmentIndex.None;
			EquipmentIndex equipmentIndex2 = EquipmentIndex.None;
			if (_spectatedAgent != null)
			{
				equipmentIndex = _spectatedAgent.GetPrimaryWieldedItemIndex();
				equipmentIndex2 = _spectatedAgent.GetOffhandWieldedItemIndex();
			}
			for (int i = 0; i < SpectatedPlayerWeapons.Count; i++)
			{
				SpectatorWeaponSlotVM spectatorWeaponSlotVM = SpectatedPlayerWeapons[i];
				bool equipped = spectatorWeaponSlotVM.SlotIndex != EquipmentIndex.None && (spectatorWeaponSlotVM.SlotIndex == equipmentIndex || spectatorWeaponSlotVM.SlotIndex == equipmentIndex2);
				spectatorWeaponSlotVM.SetEquipped(equipped);
			}
		}
	}

	private void RefreshStatNames()
	{
		for (int i = 0; i < SpectatorStats.Count; i++)
		{
			MPOverlayStatVM mPOverlayStatVM = SpectatorStats[i];
			mPOverlayStatVM.Header = GetStatName(mPOverlayStatVM.Id);
		}
	}

	private static string GetStatName(string statId)
	{
		return GameTexts.FindText("str_scoreboard_header", statId).ToString();
	}

	public void Tick(float dt)
	{
		if (_mission.MainAgent != null)
		{
			SpectatedPlayerNeutrality = -1;
		}
		IsSpectating = MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		ShowBothTeamsData = MultiplayerSpectatorHelper.ShouldShowBothTeamsData();
		UpdateDynamicProperties();
		UpdatePeerStats();
		RefreshWeaponsIfChanged();
		RefreshEquippedWeaponSlot();
		RefreshPerksIfChanged();
	}

	private void UpdateDynamicProperties()
	{
		AgentHasShield = false;
		AgentHasMount = false;
		ShowAgentHealth = false;
		AgentHasRangedWeapon = false;
		if ((SpectatedPlayerNeutrality <= 0 && !IsSpectating) || _spectatedAgent == null)
		{
			return;
		}
		ShowAgentHealth = true;
		SpectatedPlayerHealthLimit = _spectatedAgent.HealthLimit;
		SpectatedPlayerCurrentHealth = _spectatedAgent.Health;
		AgentHasMount = _spectatedAgent.MountAgent != null;
		if (AgentHasMount)
		{
			SpectatedPlayerMountCurrentHealth = _spectatedAgent.MountAgent.Health;
			SpectatedPlayerMountHealthLimit = _spectatedAgent.MountAgent.HealthLimit;
		}
		EquipmentIndex primaryWieldedItemIndex = _spectatedAgent.GetPrimaryWieldedItemIndex();
		EquipmentIndex offhandWieldedItemIndex = _spectatedAgent.GetOffhandWieldedItemIndex();
		int num = -1;
		if (primaryWieldedItemIndex != EquipmentIndex.None && _spectatedAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem != null)
		{
			if (_spectatedAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsRangedWeapon && _spectatedAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsConsumable)
			{
				int ammoAmount = _spectatedAgent.Equipment.GetAmmoAmount(primaryWieldedItemIndex);
				if (_spectatedAgent.Equipment[primaryWieldedItemIndex].ModifiedMaxAmount == 1 || ammoAmount > 0)
				{
					num = ((_spectatedAgent.Equipment[primaryWieldedItemIndex].ModifiedMaxAmount == 1) ? (-1) : ammoAmount);
				}
			}
			else if (_spectatedAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
			{
				bool flag = _spectatedAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.WeaponClass == WeaponClass.Crossbow;
				num = _spectatedAgent.Equipment.GetAmmoAmount(primaryWieldedItemIndex) + (flag ? _spectatedAgent.Equipment[primaryWieldedItemIndex].Ammo : 0);
			}
		}
		if (offhandWieldedItemIndex != EquipmentIndex.None && _spectatedAgent.Equipment[offhandWieldedItemIndex].CurrentUsageItem != null)
		{
			MissionWeapon missionWeapon = _spectatedAgent.Equipment[offhandWieldedItemIndex];
			AgentHasShield = missionWeapon.CurrentUsageItem.IsShield;
			if (AgentHasShield)
			{
				SpectatedPlayerShieldHealthLimit = missionWeapon.ModifiedMaxHitPoints;
				SpectatedPlayerShieldCurrentHealth = missionWeapon.HitPoints;
			}
		}
		AgentHasRangedWeapon = num >= 0;
		SpectatedPlayerAmmoAmount = num;
	}

	private void ClearWeaponSlots()
	{
		for (int i = 0; i < SpectatedPlayerWeapons.Count; i++)
		{
			SpectatedPlayerWeapons[i].OnFinalize();
		}
		SpectatedPlayerWeapons.Clear();
		ShowAgentWeapons = false;
	}

	private void UpdatePeerStats()
	{
		if (!(ShowAgentStats = _spectatedPeer != null))
		{
			ResetPeerStatCaches();
			SpectatorClanText = string.Empty;
			SpectatorLastKillText = string.Empty;
			SpectatorMostUsedWeaponText = string.Empty;
			return;
		}
		if (_cachedKillCount != _spectatedPeer.KillCount)
		{
			_cachedKillCount = _spectatedPeer.KillCount;
			_killCountStat.Refresh(_cachedKillCount.ToString());
		}
		if (_cachedDeathCount != _spectatedPeer.DeathCount)
		{
			_cachedDeathCount = _spectatedPeer.DeathCount;
			_deathCountStat.Refresh(_cachedDeathCount.ToString());
		}
		if (_cachedAssistCount != _spectatedPeer.AssistCount)
		{
			_cachedAssistCount = _spectatedPeer.AssistCount;
			_assistCountStat.Refresh(_cachedAssistCount.ToString());
		}
		string text = _spectatedPeer.ClanName ?? string.Empty;
		if (_cachedClanName != text)
		{
			_cachedClanName = text;
			SpectatorClanText = text;
		}
		string text2 = _spectatedPeer.LastKillVictimName ?? string.Empty;
		if (_cachedLastKillVictimName != text2)
		{
			_cachedLastKillVictimName = text2;
			if (!string.IsNullOrEmpty(text2))
			{
				TextObject textObject = new TextObject("{=jF9sZMB5}Last Kill: {VALUE}");
				textObject.SetTextVariable("VALUE", text2);
				SpectatorLastKillText = textObject.ToString();
			}
			else
			{
				SpectatorLastKillText = string.Empty;
			}
		}
		string text3 = _spectatedPeer.MostUsedWeaponName ?? string.Empty;
		if (_cachedMostUsedWeaponName != text3)
		{
			_cachedMostUsedWeaponName = text3;
			if (!string.IsNullOrEmpty(text3))
			{
				TextObject textObject2 = new TextObject("{=YviubqFI}Top Weapon: {VALUE}");
				textObject2.SetTextVariable("VALUE", text3);
				SpectatorMostUsedWeaponText = textObject2.ToString();
			}
			else
			{
				SpectatorMostUsedWeaponText = string.Empty;
			}
		}
		UpdateGold();
	}

	private void UpdateGold()
	{
		if (_goldStat == null || _spectatedPeer == null)
		{
			_cachedGold = -1;
			return;
		}
		MissionRepresentativeBase component = _spectatedPeer.GetComponent<MissionRepresentativeBase>();
		if (component == null)
		{
			_cachedGold = -1;
			_goldStat.Refresh(string.Empty);
		}
		else if (_cachedGold != component.Gold)
		{
			_cachedGold = component.Gold;
			_goldStat.Refresh(_cachedGold.ToString());
		}
	}

	internal void OnSpectatedAgentFocusIn(Agent followedAgent)
	{
		_spectatedAgent = followedAgent;
		int spectatedPlayerNeutrality = 0;
		MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		if (component != null && component.Team != _mission.SpectatorTeam && component.Team == followedAgent.Team && _isTeamsEnabled)
		{
			spectatedPlayerNeutrality = 1;
		}
		IsSpectating = MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		ShowBothTeamsData = MultiplayerSpectatorHelper.ShouldShowBothTeamsData();
		SpectatedPlayerNeutrality = spectatedPlayerNeutrality;
		SpectatedPlayerName = followedAgent.MissionPeer?.DisplayedName ?? followedAgent.Name.ToString();
		CanTakeControlOfSpectatedAgent = _isFlagDominationMode && component?.ControlledFormation != null && component.ControlledFormation == followedAgent.Formation;
		CompassElement = null;
		AgentHasCompassElement = false;
		SpectatedPlayerSigil = null;
		AgentHasSigil = false;
		MissionPeer missionPeer = (_spectatedPeer = followedAgent.MissionPeer ?? followedAgent.Formation?.PlayerOwner?.MissionPeer);
		Team team = missionPeer?.Team;
		if (missionPeer?.Peer != null && team != null)
		{
			TargetIconType iconType = MultiplayerClassDivisions.GetMPHeroClassForPeer(missionPeer)?.IconType ?? TargetIconType.None;
			Banner banner = new Banner(missionPeer.Peer.BannerCode, team.Color, team.Color2);
			CompassElement = new MPTeammateCompassTargetVM(iconType, team.Color, team.Color2, banner, team.IsPlayerAlly);
			AgentHasCompassElement = true;
			SpectatedPlayerSigil = new BannerImageIdentifierVM(banner, nineGrid: true);
			AgentHasSigil = true;
			TeamColor = Color.FromUint(team.Color);
		}
		else
		{
			TeamColor = Color.White;
		}
		ResetPeerStatCaches();
		RefreshWeaponsIfChanged();
		RefreshPerksIfChanged();
		UpdatePeerStats();
	}

	private void RefreshWeapons()
	{
		ClearWeaponSlots();
		if (!IsSpectating || _spectatedAgent == null)
		{
			return;
		}
		MissionEquipment equipment = _spectatedAgent.Equipment;
		if (equipment == null)
		{
			return;
		}
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex <= EquipmentIndex.Weapon3; equipmentIndex++)
		{
			ItemObject item = equipment[equipmentIndex].Item;
			if (item != null && item.PrimaryWeapon != null && !item.PrimaryWeapon.IsAmmo)
			{
				SpectatedPlayerWeapons.Add(new SpectatorWeaponSlotVM(item, equipmentIndex, equipment));
			}
		}
		ShowAgentWeapons = SpectatedPlayerWeapons.Count > 0;
	}

	private void RefreshWeaponsIfChanged()
	{
		if (!IsSpectating || _spectatedAgent == null)
		{
			if (SpectatedPlayerWeapons.Count > 0)
			{
				ClearWeaponSlots();
			}
			for (int i = 0; i < _cachedWeaponItems.Length; i++)
			{
				_cachedWeaponItems[i] = null;
			}
			return;
		}
		MissionEquipment equipment = _spectatedAgent.Equipment;
		bool flag = false;
		int num = 0;
		EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
		while (equipmentIndex <= EquipmentIndex.Weapon3)
		{
			ItemObject itemObject = equipment?[equipmentIndex].Item;
			if (_cachedWeaponItems[num] != itemObject)
			{
				_cachedWeaponItems[num] = itemObject;
				flag = true;
			}
			equipmentIndex++;
			num++;
		}
		if (flag)
		{
			RefreshWeapons();
		}
	}

	private void RefreshPerksIfChanged()
	{
		if (_spectatedPeer == null)
		{
			if (SpectatedPlayerPerks.Count > 0)
			{
				SpectatedPlayerPerks.Clear();
			}
			ShowAgentPerks = false;
			_cachedPerkPeer = null;
			_cachedPerkSelectedTroopIndex = -1;
		}
		else
		{
			if (_cachedPerkPeer == _spectatedPeer && _cachedPerkSelectedTroopIndex == _spectatedPeer.SelectedTroopIndex)
			{
				return;
			}
			_cachedPerkPeer = _spectatedPeer;
			_cachedPerkSelectedTroopIndex = _spectatedPeer.SelectedTroopIndex;
			SpectatedPlayerPerks.Clear();
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(_spectatedPeer);
			if (_spectatedPeer.Culture != null && mPHeroClassForPeer != null)
			{
				foreach (MPPerkObject selectedPerk in _spectatedPeer.SelectedPerks)
				{
					SpectatedPlayerPerks.Add(new MPPerkVM(null, selectedPerk, isSelectable: false, 0));
				}
			}
			ShowAgentPerks = SpectatedPlayerPerks.Count > 0;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		for (int i = 0; i < SpectatorStats.Count; i++)
		{
			SpectatorStats[i].OnFinalize();
		}
		ClearWeaponSlots();
		for (int j = 0; j < SpectatedPlayerPerks.Count; j++)
		{
			SpectatedPlayerPerks[j].OnFinalize();
		}
		CyclePreviousKey?.OnFinalize();
		CycleNextKey?.OnFinalize();
	}

	internal void OnSpectatedAgentFocusOut(Agent followedPeer)
	{
		_spectatedAgent = null;
		_spectatedPeer = null;
		ShowAgentStats = false;
		ClearWeaponSlots();
		ShowAgentPerks = false;
		SpectatedPlayerPerks.Clear();
		_cachedPerkPeer = null;
		_cachedPerkSelectedTroopIndex = -1;
		ResetPeerStatCaches();
		SpectatorClanText = string.Empty;
		SpectatorLastKillText = string.Empty;
		SpectatorMostUsedWeaponText = string.Empty;
		TeamColor = Color.White;
		SpectatedPlayerNeutrality = -1;
	}

	private void ResetPeerStatCaches()
	{
		_cachedKillCount = -1;
		_cachedDeathCount = -1;
		_cachedAssistCount = -1;
		_cachedGold = -1;
		_cachedClanName = null;
		_cachedLastKillVictimName = null;
		_cachedMostUsedWeaponName = null;
		for (int i = 0; i < SpectatorStats.Count; i++)
		{
			SpectatorStats[i].Refresh(string.Empty);
		}
	}
}
