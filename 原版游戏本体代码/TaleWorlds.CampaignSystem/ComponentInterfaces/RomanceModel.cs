using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001AE RID: 430
	public abstract class RomanceModel : MBGameModel<RomanceModel>
	{
		// Token: 0x06001CFC RID: 7420
		public abstract int GetAttractionValuePercentage(Hero potentiallyInterestedCharacter, Hero heroOfInterest);
	}
}
