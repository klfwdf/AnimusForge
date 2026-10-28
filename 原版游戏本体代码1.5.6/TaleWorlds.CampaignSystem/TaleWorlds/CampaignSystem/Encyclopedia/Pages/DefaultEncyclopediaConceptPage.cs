using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia.Pages;

[EncyclopediaModel(new Type[] { typeof(Concept) })]
public class DefaultEncyclopediaConceptPage : EncyclopediaPage
{
	public DefaultEncyclopediaConceptPage()
	{
		base.HomePageOrderIndex = 600;
	}

	protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
	{
		foreach (Concept item in Concept.All)
		{
			yield return new EncyclopediaListItem(item, item.Title.ToString(), item.Description.ToString(), item.StringId, GetIdentifier(typeof(Concept)), playerCanSeeValues: true);
		}
	}

	protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
	{
		return new List<EncyclopediaFilterGroup>
		{
			new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>
			{
				new EncyclopediaFilterItem(new TextObject("{=uauMia0D} Characters"), (object c) => Concept.IsGroupMember("Characters", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=cwRkqIt4} Kingdoms"), (object c) => Concept.IsGroupMember("Kingdoms", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=x6knoNnC} Clans"), (object c) => Concept.IsGroupMember("Clans", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=GYzkb4iB} Parties"), (object c) => Concept.IsGroupMember("Parties", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=u6GM5Spa} Armies"), (object c) => Concept.IsGroupMember("Armies", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=zPYRGJtD} Troops"), (object c) => Concept.IsGroupMember("Troops", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=3PUkH5Zf} Items"), (object c) => Concept.IsGroupMember("Items", (Concept)c)),
				new EncyclopediaFilterItem(new TextObject("{=xKVBAL3m} Campaign Issues"), (object c) => Concept.IsGroupMember("CampaignIssues", (Concept)c))
			}, new TextObject("{=tBx7XXps}Types"))
		};
	}

	protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
	{
		return new List<EncyclopediaSortController>();
	}

	public override string GetViewFullyQualifiedName()
	{
		return "EncyclopediaConceptPage";
	}

	public override TextObject GetName()
	{
		return GameTexts.FindText("str_concepts");
	}

	public override TextObject GetDescriptionText()
	{
		return GameTexts.FindText("str_concepts_description");
	}

	public override string GetStringID()
	{
		return "EncyclopediaConcept";
	}

	public override bool IsValidEncyclopediaItem(object o)
	{
		if (o is Concept concept && concept.Title != null)
		{
			return concept.Description != null;
		}
		return false;
	}
}
