using System;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPPlayerVM : ViewModel
{
	private Action<MPPlayerVM> _onSelected;

	private MultiplayerClassDivisions.MPHeroClass _cachedClass;

	private BasicCultureObject _cachedCulture;

	private readonly MissionMultiplayerGameModeBaseClient _gameMode;

	private readonly MissionRepresentativeBase _missionRepresentative;

	private readonly bool _isInParty;

	private readonly bool _isKnownPlayer;

	private TextObject _genericPlayerName = new TextObject("{=RN6zHak0}Player");

	private const uint _focusedContourColor = 4278255612u;

	private const uint _defaultContourColor = 0u;

	private const uint _invalidColor = 0u;

	private int _gold;

	private int _valuePercent;

	private string _name;

	private string _cultureID;

	private bool _isDead;

	private bool _isValueEnabled;

	private bool _hasSetCompassElement;

	private bool _isSpawnActive;

	private bool _isFocused;

	private bool _isSelectable;

	private MPTeammateCompassTargetVM _compassElement;

	private PlayerAvatarImageIdentifierVM _avatar;

	private MPArmoryHeroPreviewVM _preview;

	private MBBindingList<MPPerkVM> _activePerks;

	public MissionPeer Peer { get; private set; }

	private Team _playerTeam
	{
		get
		{
			if (!GameNetwork.IsMyPeerReady)
			{
				return null;
			}
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component.Team == null || component.Team.Side == BattleSideEnum.None)
			{
				return null;
			}
			return component.Team;
		}
	}

	[DataSourceProperty]
	public int Gold
	{
		get
		{
			return _gold;
		}
		set
		{
			if (value != _gold)
			{
				_gold = value;
				OnPropertyChangedWithValue(value, "Gold");
			}
		}
	}

	[DataSourceProperty]
	public int ValuePercent
	{
		get
		{
			return _valuePercent;
		}
		set
		{
			if (value != _valuePercent)
			{
				_valuePercent = value;
				OnPropertyChangedWithValue(value, "ValuePercent");
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
	public string CultureID
	{
		get
		{
			return _cultureID;
		}
		set
		{
			if (value != _cultureID)
			{
				_cultureID = value;
				OnPropertyChangedWithValue(value, "CultureID");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (value != _isDead)
			{
				_isDead = value;
				OnPropertyChangedWithValue(value, "IsDead");
			}
		}
	}

	[DataSourceProperty]
	public bool IsValueEnabled
	{
		get
		{
			return _isValueEnabled;
		}
		set
		{
			if (value != _isValueEnabled)
			{
				_isValueEnabled = value;
				OnPropertyChangedWithValue(value, "IsValueEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSetCompassElement
	{
		get
		{
			return _hasSetCompassElement;
		}
		set
		{
			if (value != _hasSetCompassElement)
			{
				_hasSetCompassElement = value;
				OnPropertyChangedWithValue(value, "HasSetCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpawnActive
	{
		get
		{
			return _isSpawnActive;
		}
		set
		{
			if (value != _isSpawnActive)
			{
				_isSpawnActive = value;
				OnPropertyChangedWithValue(value, "IsSpawnActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChangedWithValue(value, "IsSelectable");
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
	public PlayerAvatarImageIdentifierVM Avatar
	{
		get
		{
			return _avatar;
		}
		set
		{
			if (value != _avatar)
			{
				_avatar = value;
				OnPropertyChangedWithValue(value, "Avatar");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryHeroPreviewVM Preview
	{
		get
		{
			return _preview;
		}
		set
		{
			if (value != _preview)
			{
				_preview = value;
				OnPropertyChangedWithValue(value, "Preview");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPerkVM> ActivePerks
	{
		get
		{
			return _activePerks;
		}
		set
		{
			if (value != _activePerks)
			{
				_activePerks = value;
				OnPropertyChangedWithValue(value, "ActivePerks");
			}
		}
	}

	public MPPlayerVM(Agent agent)
	{
		if (agent != null)
		{
			TargetIconType iconType = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character)?.IconType ?? TargetIconType.None;
			uint num = agent.Team?.Color ?? 0;
			uint color = agent.Team?.Color2 ?? 0;
			Banner banner = new Banner(agent.Team.Banner, num, color);
			CompassElement = new MPTeammateCompassTargetVM(iconType, num, color, banner, isAlly: false);
		}
		else
		{
			CompassElement = new MPTeammateCompassTargetVM(TargetIconType.Monster, 0u, 0u, Banner.CreateOneColoredEmptyBanner(0), isAlly: false);
		}
	}

	public MPPlayerVM(MissionPeer peer)
	{
		Peer = peer;
		_gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_missionRepresentative = peer.GetComponent<MissionRepresentativeBase>();
		_isInParty = NetworkMain.GameClient.IsInParty;
		_isKnownPlayer = NetworkMain.GameClient.IsKnownPlayer(Peer.Peer.Id);
		RefreshAvatar();
		Name = peer.DisplayedName;
		ActivePerks = new MBBindingList<MPPerkVM>();
		RefreshValues();
	}

	public void UpdateDisabled()
	{
		IsDead = !Peer.IsControlledAgentActive;
	}

	public void RefreshDivision(bool useCultureColors = false)
	{
		if (Peer == null || Peer.Culture == null)
		{
			return;
		}
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(Peer);
		TargetIconType targetIconType = mPHeroClassForPeer?.IconType ?? TargetIconType.None;
		if (_cachedClass == null || _cachedClass != mPHeroClassForPeer || _cachedCulture == null || _cachedCulture != Peer.Culture)
		{
			_cachedClass = mPHeroClassForPeer;
			_cachedCulture = Peer.Culture;
			uint num = Peer.Team?.Color ?? 0;
			uint color = Peer.Team?.Color2 ?? 0;
			if (useCultureColors)
			{
				BasicCultureObject attackerCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
				BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
				MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = MultiplayerBattleColors.CreateWith(attackerCulture, defenderCulture).GetPeerColors(Peer);
				num = peerColors.Color1Uint;
				color = peerColors.Color2Uint;
			}
			Banner banner = new Banner(Peer.Peer.BannerCode, num, color);
			CompassElement = new MPTeammateCompassTargetVM(targetIconType, num, color, banner, Peer.Team?.IsPlayerAlly ?? false);
			HasSetCompassElement = true;
			Name = Peer.DisplayedName;
			RefreshActivePerks();
			CultureID = _cachedCulture.StringId;
		}
		CompassElement.RefreshTargetIconType(targetIconType);
	}

	public void RefreshGold()
	{
		if (Peer != null && _gameMode.IsGameModeUsingGold)
		{
			if (_missionRepresentative is FlagDominationMissionRepresentative flagDominationMissionRepresentative)
			{
				Gold = flagDominationMissionRepresentative.Gold;
				IsSpawnActive = Gold >= 100;
			}
		}
		else
		{
			IsSpawnActive = false;
		}
	}

	public void RefreshTeam()
	{
		if (Peer != null)
		{
			Banner banner = new Banner(Peer.Peer.BannerCode, Peer.Team?.Color ?? 0, Peer.Team?.Color2 ?? 0);
			CompassElement.RefreshTeam(banner, Peer.Team?.IsPlayerAlly ?? false);
			CompassElement.RefreshColor(Peer.Team?.Color ?? 0, Peer.Team?.Color2 ?? 0);
		}
	}

	public void RefreshProperties()
	{
		bool flag = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0;
		IsValueEnabled = (Peer?.Team != null && Peer.Team == _playerTeam) || flag;
		if (IsValueEnabled)
		{
			if (flag)
			{
				ValuePercent = ((Peer.BotsUnderControlTotal != 0) ? ((int)((float)Peer.BotsUnderControlAlive / (float)Peer.BotsUnderControlTotal * 100f)) : 0);
			}
			else
			{
				ValuePercent = ((Peer.ControlledAgent != null) ? TaleWorlds.Library.MathF.Ceiling(Peer.ControlledAgent.Health / Peer.ControlledAgent.HealthLimit * 100f) : 0);
			}
		}
	}

	public void RefreshPreview(BasicCharacterObject character, DynamicBodyProperties dynamicBodyProperties, bool isFemale)
	{
		Preview = new MPArmoryHeroPreviewVM();
		Preview.SetCharacter(character, dynamicBodyProperties, character.Race, isFemale);
	}

	public void RefreshActivePerks()
	{
		ActivePerks.Clear();
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(Peer);
		if (Peer == null || Peer.Culture == null || mPHeroClassForPeer == null)
		{
			return;
		}
		foreach (MPPerkObject selectedPerk in Peer.SelectedPerks)
		{
			ActivePerks.Add(new MPPerkVM(null, selectedPerk, isSelectable: false, 0));
		}
	}

	public void RefreshAvatar()
	{
		if (NetworkMain.GameClient == null)
		{
			Debug.FailedAssert("Network is not enabled when trying to refresh avatars", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\MPPlayerVM.cs", "RefreshAvatar", 208);
		}
		else if (Peer == null)
		{
			Debug.FailedAssert("Trying to refresh avatar of a player without peer!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\MPPlayerVM.cs", "RefreshAvatar", 214);
		}
		else
		{
			Avatar = new PlayerAvatarImageIdentifierVM(forcedAvatarIndex: NetworkMain.GameClient.HasUserGeneratedContentPrivilege ? ((!BannerlordConfig.EnableGenericAvatars || _isKnownPlayer) ? (-1) : AvatarServices.GetForcedAvatarIndexOfPlayer(Peer.Peer.Id)) : AvatarServices.GetForcedAvatarIndexOfPlayer(Peer.Peer.Id), playerId: Peer.Peer.Id);
		}
	}

	public void SetSelectionHandler(Action<MPPlayerVM> onSelected)
	{
		_onSelected = onSelected;
	}

	public virtual void ExecuteSelectPlayer()
	{
		_onSelected?.Invoke(this);
	}

	public void ExecuteFocusBegin()
	{
		SetFocusState(isFocused: true);
	}

	public void ExecuteFocusEnd()
	{
		SetFocusState(isFocused: false);
	}

	private void SetFocusState(bool isFocused)
	{
		uint value = (isFocused ? 4278255612u : 0u);
		if (Peer != null)
		{
			Peer.GetAgentVisualForPeer(0)?.GetCopyAgentVisualsData().AgentVisuals.SetContourColor(value);
		}
		IsFocused = isFocused;
	}
}
