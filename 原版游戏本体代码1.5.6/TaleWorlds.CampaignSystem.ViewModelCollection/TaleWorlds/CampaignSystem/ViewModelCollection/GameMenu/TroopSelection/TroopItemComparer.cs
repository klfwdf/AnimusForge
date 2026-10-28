using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;

public class TroopItemComparer : IComparer<TroopSelectionItemVM>
{
	public int Compare(TroopSelectionItemVM x, TroopSelectionItemVM y)
	{
		if (y.Troop.Character.IsPlayerCharacter)
		{
			return 1;
		}
		if (y.Troop.Character.IsHero)
		{
			if (x.Troop.Character.IsPlayerCharacter)
			{
				return -1;
			}
			if (x.Troop.Character.IsHero)
			{
				return y.Troop.Character.Level - x.Troop.Character.Level;
			}
			return 1;
		}
		if (x.Troop.Character.IsPlayerCharacter || x.Troop.Character.IsHero)
		{
			return -1;
		}
		return y.Troop.Character.Level - x.Troop.Character.Level;
	}
}
