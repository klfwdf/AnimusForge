using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.General;

public class MPGeneralKillNotificationItemVM : ViewModel
{
	private readonly Action<MPGeneralKillNotificationItemVM> _onRemove;

	private readonly Banner _defaultBanner = Banner.CreateOneColoredEmptyBanner(92);

	private string _murdererName;

	private string _victimName;

	private MPTeammateCompassTargetVM _murdererCompassElement;

	private MPTeammateCompassTargetVM _victimCompassElement;

	private bool _isRelatedToPlayer;

	private bool _isMurdererAlly;

	private bool _isVictimAlly;

	private string _killWeaponSprite;

	private bool _showKillWeapon;

	private string _message;

	[DataSourceProperty]
	public string MurdererName
	{
		get
		{
			return _murdererName;
		}
		set
		{
			if (value != _murdererName)
			{
				_murdererName = value;
				OnPropertyChangedWithValue(value, "MurdererName");
			}
		}
	}

	[DataSourceProperty]
	public string VictimName
	{
		get
		{
			return _victimName;
		}
		set
		{
			if (value != _victimName)
			{
				_victimName = value;
				OnPropertyChangedWithValue(value, "VictimName");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM MurdererCompassElement
	{
		get
		{
			return _murdererCompassElement;
		}
		set
		{
			if (value != _murdererCompassElement)
			{
				_murdererCompassElement = value;
				OnPropertyChangedWithValue(value, "MurdererCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM VictimCompassElement
	{
		get
		{
			return _victimCompassElement;
		}
		set
		{
			if (value != _victimCompassElement)
			{
				_victimCompassElement = value;
				OnPropertyChangedWithValue(value, "VictimCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRelatedToPlayer
	{
		get
		{
			return _isRelatedToPlayer;
		}
		set
		{
			if (value != _isRelatedToPlayer)
			{
				_isRelatedToPlayer = value;
				OnPropertyChangedWithValue(value, "IsRelatedToPlayer");
			}
		}
	}

	[DataSourceProperty]
	public string KillWeaponSprite
	{
		get
		{
			return _killWeaponSprite;
		}
		set
		{
			if (value != _killWeaponSprite)
			{
				_killWeaponSprite = value;
				OnPropertyChangedWithValue(value, "KillWeaponSprite");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowKillWeapon
	{
		get
		{
			return _showKillWeapon;
		}
		set
		{
			if (value != _showKillWeapon)
			{
				_showKillWeapon = value;
				OnPropertyChangedWithValue(value, "ShowKillWeapon");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMurdererAlly
	{
		get
		{
			return _isMurdererAlly;
		}
		set
		{
			if (value != _isMurdererAlly)
			{
				_isMurdererAlly = value;
				OnPropertyChangedWithValue(value, "IsMurdererAlly");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVictimAlly
	{
		get
		{
			return _isVictimAlly;
		}
		set
		{
			if (value != _isVictimAlly)
			{
				_isVictimAlly = value;
				OnPropertyChangedWithValue(value, "IsVictimAlly");
			}
		}
	}

	[DataSourceProperty]
	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChangedWithValue(value, "Message");
			}
		}
	}

	public MPGeneralKillNotificationItemVM(Agent affectedAgent, Agent affectorAgent, Agent assistedAgent, Action<MPGeneralKillNotificationItemVM> onRemove, WeaponClass killWeaponClass = WeaponClass.Undefined)
	{
		_onRemove = onRemove;
		InitProperties(affectedAgent, affectorAgent);
		InitWeaponProperties(killWeaponClass);
		InitDeathProperties(affectedAgent, affectorAgent, assistedAgent);
	}

	private void InitWeaponProperties(WeaponClass killWeaponClass)
	{
		string weaponClassSpriteName = GetWeaponClassSpriteName(killWeaponClass);
		ShowKillWeapon = weaponClassSpriteName != null;
		KillWeaponSprite = weaponClassSpriteName ?? string.Empty;
	}

	private static string GetWeaponClassSpriteName(WeaponClass weaponClass)
	{
		switch (weaponClass)
		{
		case WeaponClass.Dagger:
		case WeaponClass.OneHandedSword:
		case WeaponClass.OneHandedAxe:
		case WeaponClass.Mace:
		case WeaponClass.Pick:
			return "General\\EquipmentIcons\\equipment_type_one_handed";
		case WeaponClass.TwoHandedSword:
		case WeaponClass.TwoHandedAxe:
		case WeaponClass.TwoHandedMace:
			return "General\\EquipmentIcons\\equipment_type_two_handed";
		case WeaponClass.OneHandedPolearm:
		case WeaponClass.TwoHandedPolearm:
		case WeaponClass.LowGripPolearm:
			return "General\\EquipmentIcons\\equipment_type_polearm";
		case WeaponClass.Arrow:
		case WeaponClass.Bow:
			return "General\\EquipmentIcons\\equipment_type_bow";
		case WeaponClass.Bolt:
		case WeaponClass.Crossbow:
			return "General\\EquipmentIcons\\equipment_type_crossbow";
		case WeaponClass.SlingStone:
		case WeaponClass.Sling:
			return "General\\EquipmentIcons\\equipment_type_sling";
		case WeaponClass.Stone:
		case WeaponClass.ThrowingAxe:
		case WeaponClass.ThrowingKnife:
		case WeaponClass.Javelin:
			return "General\\EquipmentIcons\\equipment_type_throwing";
		default:
			return null;
		}
	}

	public virtual void InitProperties(Agent affectedAgent, Agent affectorAgent)
	{
		GetAgentColors(affectorAgent, out var color, out var color2);
		TargetIconType multiplayerAgentType = GetMultiplayerAgentType(affectorAgent);
		Banner agentBanner = GetAgentBanner(affectorAgent);
		bool flag = affectorAgent?.Team?.IsPlayerAlly ?? false;
		GetAgentColors(affectedAgent, out var color3, out var color4);
		TargetIconType multiplayerAgentType2 = GetMultiplayerAgentType(affectedAgent);
		Banner agentBanner2 = GetAgentBanner(affectedAgent);
		bool flag2 = affectedAgent.Team?.IsPlayerAlly ?? false;
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (MultiplayerSpectatorHelper.ShouldShowBothTeamsData())
		{
			BattleSideEnum? battleSideEnum = affectorAgent?.Team?.Side;
			BattleSideEnum? battleSideEnum2 = affectedAgent.Team?.Side;
			IsMurdererAlly = battleSideEnum.HasValue && battleSideEnum.Value == BattleSideEnum.Attacker;
			IsVictimAlly = battleSideEnum2.HasValue && battleSideEnum2.Value == BattleSideEnum.Defender;
		}
		else if (missionPeer?.Team != null)
		{
			IsMurdererAlly = flag && !flag2;
			IsVictimAlly = flag2;
		}
		else
		{
			IsMurdererAlly = true;
			IsVictimAlly = false;
		}
		MurdererName = ((affectorAgent == null) ? "" : ((affectorAgent.MissionPeer != null) ? affectorAgent.MissionPeer.DisplayedName : affectorAgent.Name));
		MurdererCompassElement = new MPTeammateCompassTargetVM(multiplayerAgentType, color, color2, agentBanner, flag);
		VictimName = ((affectedAgent.MissionPeer != null) ? affectedAgent.MissionPeer.DisplayedName : affectedAgent.Name);
		VictimCompassElement = new MPTeammateCompassTargetVM(multiplayerAgentType2, color3, color4, agentBanner2, flag2);
		IsRelatedToPlayer = affectedAgent.IsPlayerUnit || (affectorAgent?.IsPlayerUnit ?? false);
	}

	public void InitDeathProperties(Agent affectedAgent, Agent affectorAgent, Agent assistedAgent)
	{
		if (affectorAgent != null && affectorAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", affectedAgent.NameTextObject.ToString());
			Message = GameTexts.FindText("str_kill_feed_message").ToString();
		}
		else if (affectedAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", ((object)affectorAgent)?.ToString());
			Message = GameTexts.FindText("str_death_feed_message").ToString();
		}
		else if (assistedAgent != null && assistedAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", affectedAgent.NameTextObject.ToString());
			Message = GameTexts.FindText("str_assist_feed_message").ToString();
		}
	}

	protected TargetIconType GetMultiplayerAgentType(Agent agent)
	{
		if (agent == null)
		{
			return TargetIconType.None;
		}
		if (!agent.IsHuman)
		{
			return TargetIconType.Monster;
		}
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
		if (mPHeroClassForCharacter == null)
		{
			Debug.FailedAssert("Hero class is not set for agent: " + agent.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\KillFeed\\General\\MPGeneralKillNotificationItemVM.cs", "GetMultiplayerAgentType", 142);
			return TargetIconType.None;
		}
		return mPHeroClassForCharacter.IconType;
	}

	private Banner GetAgentBanner(Agent agent)
	{
		Banner result = _defaultBanner;
		if (agent != null)
		{
			MissionPeer missionPeer = agent.MissionPeer?.GetComponent<MissionPeer>();
			if (agent.Team != null && missionPeer != null)
			{
				result = new Banner(missionPeer.Peer.BannerCode, agent.Team.Color, agent.Team.Color2);
			}
			else if (agent.Team != null && agent.Formation != null && !string.IsNullOrEmpty(agent.Formation.BannerCode))
			{
				result = new Banner(agent.Formation.BannerCode, agent.Team.Color, agent.Team.Color2);
			}
			else if (agent.Team != null)
			{
				result = agent.Team.Banner;
			}
		}
		return result;
	}

	private void GetAgentColors(Agent agent, out uint color1, out uint color2)
	{
		if (agent?.Team != null)
		{
			color1 = agent.Team.Color;
			color2 = agent.Team.Color2;
		}
		else
		{
			color1 = 4284111450u;
			color2 = uint.MaxValue;
		}
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
