using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionBasicTeamLogic : MissionLogic
{
	public override void EarlyStart()
	{
		base.EarlyStart();
		InitializeTeams();
	}

	private void GetTeamColor(BattleSideEnum side, bool isPlayerAttacker, out uint teamColor1, out uint teamColor2)
	{
		teamColor1 = uint.MaxValue;
		teamColor2 = uint.MaxValue;
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
		{
			return;
		}
		if ((isPlayerAttacker && side == BattleSideEnum.Attacker) || (!isPlayerAttacker && side == BattleSideEnum.Defender))
		{
			teamColor1 = Hero.MainHero.MapFaction.Color;
			teamColor2 = Hero.MainHero.MapFaction.Color2;
		}
		else if (MobileParty.MainParty.MapEvent != null)
		{
			if (MobileParty.MainParty.MapEvent.MapEventSettlement != null)
			{
				teamColor1 = MobileParty.MainParty.MapEvent.MapEventSettlement.MapFaction.Color;
				teamColor2 = MobileParty.MainParty.MapEvent.MapEventSettlement.MapFaction.Color2;
			}
			else
			{
				teamColor1 = MobileParty.MainParty.MapEvent.GetLeaderParty(side).MapFaction.Color;
				teamColor2 = MobileParty.MainParty.MapEvent.GetLeaderParty(side).MapFaction.Color2;
			}
		}
	}

	private void InitializeTeams(bool isPlayerAttacker = true)
	{
		if (!base.Mission.Teams.IsEmpty())
		{
			throw new MBIllegalValueException("Number of teams is not 0.");
		}
		GetTeamColor(BattleSideEnum.Defender, isPlayerAttacker, out var teamColor, out var teamColor2);
		GetTeamColor(BattleSideEnum.Attacker, isPlayerAttacker, out var teamColor3, out var teamColor4);
		base.Mission.Teams.Add(BattleSideEnum.Defender, teamColor, teamColor2);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, teamColor3, teamColor4);
		if (isPlayerAttacker)
		{
			base.Mission.Teams.Add(BattleSideEnum.Attacker);
			base.Mission.PlayerTeam = base.Mission.AttackerTeam;
		}
		else
		{
			base.Mission.Teams.Add(BattleSideEnum.Defender);
			base.Mission.PlayerTeam = base.Mission.DefenderTeam;
		}
	}
}
