using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection
{
	// Token: 0x020000A1 RID: 161
	public class TroopItemComparer : IComparer<TroopSelectionItemVM>
	{
		// Token: 0x06000FA5 RID: 4005 RVA: 0x0004090C File Offset: 0x0003EB0C
		public int Compare(TroopSelectionItemVM x, TroopSelectionItemVM y)
		{
			int result;
			if (y.Troop.Character.IsPlayerCharacter)
			{
				result = 1;
			}
			else if (y.Troop.Character.IsHero)
			{
				if (x.Troop.Character.IsPlayerCharacter)
				{
					result = -1;
				}
				else if (x.Troop.Character.IsHero)
				{
					result = y.Troop.Character.Level - x.Troop.Character.Level;
				}
				else
				{
					result = 1;
				}
			}
			else if (x.Troop.Character.IsPlayerCharacter || x.Troop.Character.IsHero)
			{
				result = -1;
			}
			else
			{
				result = y.Troop.Character.Level - x.Troop.Character.Level;
			}
			return result;
		}
	}
}
