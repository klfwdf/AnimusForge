using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability;

public class BattleTypeEnumResolver : IEnumResolver
{
	public string ResolveObject(string originalObject)
	{
		if (string.IsNullOrEmpty(originalObject))
		{
			Debug.FailedAssert("EndCaptivityDetail data is null or empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\SaveCompability\\BattleTypeEnumResolver.cs", "ResolveObject", 16);
			return MapEvent.BattleTypes.None.ToString();
		}
		if (originalObject.Equals("AlleyFight"))
		{
			return MapEvent.BattleTypes.None.ToString();
		}
		return originalObject;
	}
}
