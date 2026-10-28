using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class TroopRoleCondition : MPPerkCondition
{
	private enum Role
	{
		Sergeant,
		Troop,
		BannerBearer
	}

	protected static string StringType = "TroopRole";

	private Role _role;

	public override PerkEventFlags EventFlags => PerkEventFlags.PeerControlledAgentChange;

	protected TroopRoleCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		string text = node?.Attributes?["role"]?.Value;
		_role = Role.Sergeant;
		if (text != null && !Enum.TryParse<Role>(text, ignoreCase: true, out _role))
		{
			_role = Role.Sergeant;
			Debug.FailedAssert("provided 'role' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\TroopRoleCondition.cs", "Deserialize", 35);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		if (agent != null && MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			switch (_role)
			{
			case Role.Sergeant:
				return IsAgentSergeant(agent);
			case Role.BannerBearer:
				if (IsAgentBannerBearer(agent))
				{
					return !IsAgentSergeant(agent);
				}
				return false;
			case Role.Troop:
				if (!IsAgentBannerBearer(agent))
				{
					return !IsAgentSergeant(agent);
				}
				return false;
			}
		}
		return false;
	}

	private bool IsAgentSergeant(Agent agent)
	{
		return agent.Character == MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character).HeroCharacter;
	}

	private bool IsAgentBannerBearer(Agent agent)
	{
		MissionPeer missionPeer = agent?.MissionPeer ?? agent?.OwningAgentMissionPeer;
		Formation formation = missionPeer?.ControlledFormation;
		if (formation != null)
		{
			MissionWeapon missionWeapon = agent.Equipment[EquipmentIndex.ExtraWeaponSlot];
			if (!missionWeapon.IsEmpty && missionWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Banner && new Banner(formation.BannerCode, missionPeer.Team.Color, missionPeer.Team.Color2).Serialize() == missionWeapon.Banner.Serialize())
			{
				return true;
			}
		}
		return false;
	}
}
