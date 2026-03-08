using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000076 RID: 118
	public class MissionBasicTeamLogic : MissionLogic
	{
		// Token: 0x060004C8 RID: 1224 RVA: 0x0001E935 File Offset: 0x0001CB35
		public override void EarlyStart()
		{
			base.EarlyStart();
			this.InitializeTeams(true);
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x0001E944 File Offset: 0x0001CB44
		private void GetTeamColor(BattleSideEnum side, bool isPlayerAttacker, out uint teamColor1, out uint teamColor2)
		{
			teamColor1 = uint.MaxValue;
			teamColor2 = uint.MaxValue;
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				if ((isPlayerAttacker && side == BattleSideEnum.Attacker) || (!isPlayerAttacker && side == BattleSideEnum.Defender))
				{
					teamColor1 = Hero.MainHero.MapFaction.Color;
					teamColor2 = Hero.MainHero.MapFaction.Color2;
					return;
				}
				if (MobileParty.MainParty.MapEvent != null)
				{
					if (MobileParty.MainParty.MapEvent.MapEventSettlement != null)
					{
						teamColor1 = MobileParty.MainParty.MapEvent.MapEventSettlement.MapFaction.Color;
						teamColor2 = MobileParty.MainParty.MapEvent.MapEventSettlement.MapFaction.Color2;
						return;
					}
					teamColor1 = MobileParty.MainParty.MapEvent.GetLeaderParty(side).MapFaction.Color;
					teamColor2 = MobileParty.MainParty.MapEvent.GetLeaderParty(side).MapFaction.Color2;
				}
			}
		}

		// Token: 0x060004CA RID: 1226 RVA: 0x0001EA2C File Offset: 0x0001CC2C
		private void InitializeTeams(bool isPlayerAttacker = true)
		{
			if (!base.Mission.Teams.IsEmpty<Team>())
			{
				throw new MBIllegalValueException("Number of teams is not 0.");
			}
			uint color;
			uint color2;
			this.GetTeamColor(BattleSideEnum.Defender, isPlayerAttacker, out color, out color2);
			uint color3;
			uint color4;
			this.GetTeamColor(BattleSideEnum.Attacker, isPlayerAttacker, out color3, out color4);
			base.Mission.Teams.Add(BattleSideEnum.Defender, color, color2, null, true, false, true);
			base.Mission.Teams.Add(BattleSideEnum.Attacker, color3, color4, null, true, false, true);
			if (isPlayerAttacker)
			{
				base.Mission.Teams.Add(BattleSideEnum.Attacker, uint.MaxValue, uint.MaxValue, null, true, false, true);
				base.Mission.PlayerTeam = base.Mission.AttackerTeam;
				return;
			}
			base.Mission.Teams.Add(BattleSideEnum.Defender, uint.MaxValue, uint.MaxValue, null, true, false, true);
			base.Mission.PlayerTeam = base.Mission.DefenderTeam;
		}
	}
}
