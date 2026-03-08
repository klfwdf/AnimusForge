using System;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000019 RID: 25
	public class HealMainHeroCheat : GameplayCheatItem
	{
		// Token: 0x0600004B RID: 75 RVA: 0x00003F3D File Offset: 0x0000213D
		public override void ExecuteCheat()
		{
			if (Agent.Main != null)
			{
				Agent.Main.Health = Agent.Main.HealthLimit;
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003F5A File Offset: 0x0000215A
		public override TextObject GetName()
		{
			return new TextObject("{=PsmnVIcb}Heal Main Hero", null);
		}
	}
}
