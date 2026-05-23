using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x02000022 RID: 34
	[DefaultView]
	public class MissionSoundParametersView : MissionView
	{
		// Token: 0x060000D7 RID: 215 RVA: 0x0000A71B File Offset: 0x0000891B
		public override void EarlyStart()
		{
			base.EarlyStart();
			this.InitializeGlobalParameters();
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000A729 File Offset: 0x00008929
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			SoundManager.SetGlobalParameter("MissionCulture", 0f);
			SoundManager.SetGlobalParameter("MissionProsperity", 0f);
			SoundManager.SetGlobalParameter("MissionCombatMode", 0f);
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000A75E File Offset: 0x0000895E
		public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
		{
			this.InitializeCombatModeParameter();
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000A766 File Offset: 0x00008966
		private void InitializeGlobalParameters()
		{
			this.InitializeCultureParameter();
			this.InitializeProsperityParameter();
			this.InitializeCombatModeParameter();
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000A77C File Offset: 0x0000897C
		private void InitializeCultureParameter()
		{
			MissionSoundParametersView.SoundParameterMissionCulture soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.None;
			if (Campaign.Current != null)
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				if (currentSettlement != null)
				{
					if (currentSettlement.IsHideout)
					{
						soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Bandit;
					}
					else
					{
						string stringId = currentSettlement.Culture.StringId;
						uint num = <PrivateImplementationDetails>.ComputeStringHash(stringId);
						if (num <= 2848701557U)
						{
							if (num != 744444005U)
							{
								if (num != 1759932477U)
								{
									if (num == 2848701557U)
									{
										if (stringId == "khuzait")
										{
											soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Khuzait;
										}
									}
								}
								else if (stringId == "battania")
								{
									soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Battania;
								}
							}
							else if (stringId == "empire")
							{
								soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Empire;
							}
						}
						else if (num <= 3015521580U)
						{
							if (num != 2894801972U)
							{
								if (num == 3015521580U)
								{
									if (stringId == "aserai")
									{
										soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Aserai;
									}
								}
							}
							else if (stringId == "nord")
							{
								soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Nord;
							}
						}
						else if (num != 3311783860U)
						{
							if (num == 4214512470U)
							{
								if (stringId == "vlandia")
								{
									soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Vlandia;
								}
							}
						}
						else if (stringId == "sturgia")
						{
							soundParameterMissionCulture = MissionSoundParametersView.SoundParameterMissionCulture.Sturgia;
						}
					}
				}
			}
			SoundManager.SetGlobalParameter("MissionCulture", (float)soundParameterMissionCulture);
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000A8B0 File Offset: 0x00008AB0
		private void InitializeProsperityParameter()
		{
			MissionSoundParametersView.SoundParameterMissionProsperityLevel soundParameterMissionProsperityLevel = MissionSoundParametersView.SoundParameterMissionProsperityLevel.None;
			if (Campaign.Current != null && Settlement.CurrentSettlement != null)
			{
				switch (Settlement.CurrentSettlement.SettlementComponent.GetProsperityLevel())
				{
				case SettlementComponent.ProsperityLevel.Low:
					soundParameterMissionProsperityLevel = MissionSoundParametersView.SoundParameterMissionProsperityLevel.None;
					break;
				case SettlementComponent.ProsperityLevel.Mid:
					soundParameterMissionProsperityLevel = MissionSoundParametersView.SoundParameterMissionProsperityLevel.Mid;
					break;
				case SettlementComponent.ProsperityLevel.High:
					soundParameterMissionProsperityLevel = MissionSoundParametersView.SoundParameterMissionProsperityLevel.High;
					break;
				}
			}
			SoundManager.SetGlobalParameter("MissionProsperity", (float)soundParameterMissionProsperityLevel);
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000A908 File Offset: 0x00008B08
		private void InitializeCombatModeParameter()
		{
			bool flag = base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel || base.Mission.Mode == MissionMode.Tournament;
			SoundManager.SetGlobalParameter("MissionCombatMode", (float)(flag ? 1 : 0));
		}

		// Token: 0x0400007E RID: 126
		private const string CultureParameterId = "MissionCulture";

		// Token: 0x0400007F RID: 127
		private const string ProsperityParameterId = "MissionProsperity";

		// Token: 0x04000080 RID: 128
		private const string CombatParameterId = "MissionCombatMode";

		// Token: 0x0200008D RID: 141
		public enum SoundParameterMissionCulture : short
		{
			// Token: 0x040002CB RID: 715
			None,
			// Token: 0x040002CC RID: 716
			Aserai,
			// Token: 0x040002CD RID: 717
			Battania,
			// Token: 0x040002CE RID: 718
			Empire,
			// Token: 0x040002CF RID: 719
			Khuzait,
			// Token: 0x040002D0 RID: 720
			Sturgia,
			// Token: 0x040002D1 RID: 721
			Vlandia,
			// Token: 0x040002D2 RID: 722
			Nord,
			// Token: 0x040002D3 RID: 723
			ReservedA,
			// Token: 0x040002D4 RID: 724
			ReservedB,
			// Token: 0x040002D5 RID: 725
			Bandit
		}

		// Token: 0x0200008E RID: 142
		private enum SoundParameterMissionProsperityLevel : short
		{
			// Token: 0x040002D7 RID: 727
			None,
			// Token: 0x040002D8 RID: 728
			Low = 0,
			// Token: 0x040002D9 RID: 729
			Mid,
			// Token: 0x040002DA RID: 730
			High
		}
	}
}
