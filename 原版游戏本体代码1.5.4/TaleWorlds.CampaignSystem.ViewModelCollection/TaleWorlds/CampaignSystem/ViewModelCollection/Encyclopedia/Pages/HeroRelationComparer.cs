using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

public class HeroRelationComparer : IComparer<HeroVM>
{
	private readonly Hero _pageHero;

	private readonly bool _isAscending;

	private readonly bool _showLeadersFirst;

	public HeroRelationComparer(Hero pageHero, bool isAscending, bool showLeadersFirst)
	{
		_pageHero = pageHero;
		_isAscending = isAscending;
		_showLeadersFirst = showLeadersFirst;
	}

	int IComparer<HeroVM>.Compare(HeroVM x, HeroVM y)
	{
		int num;
		if (_showLeadersFirst)
		{
			num = y.IsKingdomLeader.CompareTo(x.IsKingdomLeader);
			if (num != 0)
			{
				return num;
			}
		}
		int relation = _pageHero.GetRelation(x.Hero);
		int relation2 = _pageHero.GetRelation(y.Hero);
		num = relation.CompareTo(relation2) * (_isAscending ? 1 : (-1));
		if (num == 0)
		{
			num = x.NameText.CompareTo(y.NameText);
		}
		return num;
	}
}
