using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public class EncyclopediaSortController
{
	public TextObject Name { get; }

	public EncyclopediaListItemComparerBase Comparer { get; }

	public EncyclopediaSortController(TextObject name, EncyclopediaListItemComparerBase comparer)
	{
		Name = name;
		Comparer = comparer;
	}
}
