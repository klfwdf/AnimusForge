using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability;

public class ArmyDispersionReasonEnumResolver : IEnumResolver
{
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
