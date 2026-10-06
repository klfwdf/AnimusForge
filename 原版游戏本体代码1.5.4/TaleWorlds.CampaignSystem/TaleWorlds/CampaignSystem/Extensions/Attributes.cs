using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class Attributes
{
	public static MBReadOnlyList<CharacterAttribute> All => Campaign.Current.AllCharacterAttributes;
}
