using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

public class HeroAgeComparer : IComparer<HeroVM>
{
	private readonly bool _isAscending;

	public HeroAgeComparer(bool isAscending)
	{
		_isAscending = isAscending;
	}

	int IComparer<HeroVM>.Compare(HeroVM x, HeroVM y)
	{
		int num = x.Hero.Age.CompareTo(y.Hero.Age) * (_isAscending ? 1 : (-1));
		if (num == 0)
		{
			num = x.NameText.CompareTo(y.NameText);
		}
		return num;
	}
}
