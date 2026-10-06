using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;

public class LeaveKingdomPermissionEvent : EventBase
{
	public Action<bool, TextObject> IsLeaveKingdomPossbile { get; private set; }

	public LeaveKingdomPermissionEvent(Action<bool, TextObject> isLeaveKingdomPossbile)
	{
		IsLeaveKingdomPossbile = isLeaveKingdomPossbile;
	}
}
