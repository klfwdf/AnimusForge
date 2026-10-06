using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.Localization;

namespace NavalDLC.Encyclopedia.Pages;

[OverrideEncyclopediaModel(new Type[] { typeof(CharacterObject) })]
public class NavalEncyclopediaUnityPage : DefaultEncyclopediaUnitPage
{
	protected override List<EncyclopediaFilterItem> GetTypeFilterItems()
	{
		List<EncyclopediaFilterItem> typeFilterItems = base.GetTypeFilterItems();
		typeFilterItems.Add(new EncyclopediaFilterItem(new TextObject("{=bOhiqquf}Mariner"), (object s) => ((CharacterObject)s).IsMariner));
		return typeFilterItems;
	}
}
