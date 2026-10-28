using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public readonly struct ClanCardSelectionInfo(TextObject title, IEnumerable<ClanCardSelectionItemInfo> items, Action<List<object>, Action> onClosedAction, bool isMultiSelection, int minimumSelection = 1, int maximumSelection = 0)
{
	public readonly TextObject Title = title;

	public readonly IEnumerable<ClanCardSelectionItemInfo> Items = items;

	public readonly Action<List<object>, Action> OnClosedAction = onClosedAction;

	public readonly bool IsMultiSelection = isMultiSelection;

	public readonly int MinimumSelection = minimumSelection;

	public readonly int MaximumSelection = maximumSelection;
}
