using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;

public class SettlementOverlayLeaveCharacterPermissionEvent : EventBase
{
	public Action<bool, TextObject> IsLeaveAvailable { get; private set; }

	public SettlementOverlayLeaveCharacterPermissionEvent(Action<bool, TextObject> isLeaveAvailable)
	{
		IsLeaveAvailable = isLeaveAvailable;
	}
}
