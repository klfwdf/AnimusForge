using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C6 RID: 454
	public abstract class DisguiseDetectionModel : MBGameModel<DisguiseDetectionModel>
	{
		// Token: 0x06001DBB RID: 7611
		public abstract float CalculateDisguiseDetectionProbability(Settlement settlement);
	}
}
