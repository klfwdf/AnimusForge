using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000CC RID: 204
	public class ArmyDispersionReasonEnumResolver : IEnumResolver
	{
		// Token: 0x06001449 RID: 5193 RVA: 0x0005E190 File Offset: 0x0005C390
		public string ResolveObject(string originalObject)
		{
			if (string.IsNullOrEmpty(originalObject))
			{
				Debug.FailedAssert("ArmyDispersionReason data is null or empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\SaveCompability\\ArmyDispersionReasonEnumResolver.cs", "ResolveObject", 16);
				return Army.ArmyDispersionReason.Unknown.ToString();
			}
			if (originalObject.Equals("LowPartySizeRatio"))
			{
				return Army.ArmyDispersionReason.NotEnoughTroop.ToString();
			}
			return originalObject;
		}
	}
}
