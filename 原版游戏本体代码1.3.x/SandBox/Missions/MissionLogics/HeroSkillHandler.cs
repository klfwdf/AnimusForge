using System;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200006B RID: 107
	public class HeroSkillHandler : MissionLogic
	{
		// Token: 0x06000461 RID: 1121 RVA: 0x0001A5F4 File Offset: 0x000187F4
		public override void AfterStart()
		{
			this._nextCaptainSkillMoraleBoostTime = MissionTime.SecondsFromNow(10f);
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x0001A608 File Offset: 0x00018808
		public override void OnMissionTick(float dt)
		{
			if (this._nextCaptainSkillMoraleBoostTime.IsPast)
			{
				this._boostMorale = true;
				this._nextMoraleTeam = 0;
				this._nextCaptainSkillMoraleBoostTime = MissionTime.SecondsFromNow(10f);
			}
			if (this._boostMorale)
			{
				if (this._nextMoraleTeam >= base.Mission.Teams.Count)
				{
					this._boostMorale = false;
					return;
				}
				Team team = base.Mission.Teams[this._nextMoraleTeam];
				this.BoostMoraleForTeam(team);
				this._nextMoraleTeam++;
			}
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x0001A694 File Offset: 0x00018894
		private void BoostMoraleForTeam(Team team)
		{
		}

		// Token: 0x04000259 RID: 601
		private MissionTime _nextCaptainSkillMoraleBoostTime;

		// Token: 0x0400025A RID: 602
		private bool _boostMorale;

		// Token: 0x0400025B RID: 603
		private int _nextMoraleTeam;
	}
}
