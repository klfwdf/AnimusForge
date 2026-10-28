using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;

public class SettlementOverylayQuickTalkPermissionEvent : EventBase
{
	public Hero HeroToTalkTo;

	public Action<bool, TextObject> IsTalkAvailable { get; private set; }

	public SettlementOverylayQuickTalkPermissionEvent(Hero heroToTalkTo, Action<bool, TextObject> isTalkAvailable)
	{
		HeroToTalkTo = heroToTalkTo;
		IsTalkAvailable = isTalkAvailable;
	}
}
