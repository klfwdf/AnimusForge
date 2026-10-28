using System.Xml;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class BannerBearerCondition : MPPerkCondition
{
	protected static string StringType = "BannerBearer";

	public override PerkEventFlags EventFlags => PerkEventFlags.AliveBotCountChange | PerkEventFlags.BannerPickUp | PerkEventFlags.BannerDrop | PerkEventFlags.SpawnEnd;

	public override bool IsPeerCondition => true;

	protected BannerBearerCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
	}

	public override bool Check(MissionPeer peer)
	{
		Formation formation = peer?.ControlledFormation;
		if (formation != null && MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			foreach (IFormationUnit allUnit in formation.Arrangement.GetAllUnits())
			{
				if (allUnit is Agent agent && agent.IsActive())
				{
					MissionWeapon missionWeapon = agent.Equipment[EquipmentIndex.ExtraWeaponSlot];
					if (!missionWeapon.IsEmpty && missionWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Banner && new Banner(formation.BannerCode, peer.Team.Color, peer.Team.Color2).Serialize() == missionWeapon.Banner.Serialize())
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		MissionPeer peer = agent?.MissionPeer ?? agent?.OwningAgentMissionPeer;
		return Check(peer);
	}
}
