using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public readonly struct ClanCardSelectionInfo
{
	public readonly TextObject Title;

	public readonly IEnumerable<ClanCardSelectionItemInfo> Items;

	public readonly Action<List<object>, Action> OnClosedAction;

	public readonly bool IsMultiSelection;

	public readonly int MinimumSelection;

	public readonly int MaximumSelection;

	public ClanCardSelectionInfo(TextObject title, IEnumerable<ClanCardSelectionItemInfo> items, Action<List<object>, Action> onClosedAction, bool isMultiSelection, int minimumSelection = 1, int maximumSelection = 0)
	{
		Title = title;
		Items = items;
		OnClosedAction = onClosedAction;
		IsMultiSelection = isMultiSelection;
		MinimumSelection = minimumSelection;
		MaximumSelection = maximumSelection;
	}
}
