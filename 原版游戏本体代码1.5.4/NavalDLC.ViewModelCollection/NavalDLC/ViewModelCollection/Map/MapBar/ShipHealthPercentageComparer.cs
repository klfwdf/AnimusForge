using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection.Map.MapBar;

public class ShipHealthPercentageComparer : IComparer<Ship>
{
	public int Compare(Ship x, Ship y)
	{
		int num = MathF.Ceiling(y.GetHealthPercent()).CompareTo(MathF.Ceiling(x.GetHealthPercent()));
		if (num != 0)
		{
			return num;
		}
		return ResolveEquality(x, y);
	}

	private int ResolveEquality(Ship x, Ship y)
	{
		return x.Name.ToString().CompareTo(y.Name.ToString());
	}
}
