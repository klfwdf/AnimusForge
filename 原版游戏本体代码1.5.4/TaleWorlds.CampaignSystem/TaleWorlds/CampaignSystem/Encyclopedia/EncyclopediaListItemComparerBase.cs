using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public abstract class EncyclopediaListItemComparerBase : IComparer<EncyclopediaListItem>
{
	protected readonly TextObject _emptyValue = new TextObject("{=4NaOKslb}-");

	protected readonly TextObject _missingValue = new TextObject("{=keqS2dGa}???");

	public bool IsAscending { get; private set; }

	public void SetSortOrder(bool isAscending)
	{
		IsAscending = isAscending;
	}

	public void SwitchSortOrder()
	{
		IsAscending = !IsAscending;
	}

	public void SetDefaultSortOrder()
	{
		IsAscending = false;
	}

	public abstract int Compare(EncyclopediaListItem x, EncyclopediaListItem y);

	public abstract string GetComparedValueText(EncyclopediaListItem item);

	protected int ResolveEquality(EncyclopediaListItem x, EncyclopediaListItem y)
	{
		return x.Name.CompareTo(y.Name);
	}
}
