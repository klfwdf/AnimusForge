using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;

public class PartyScreenCharacterTalkPermissionEvent : EventBase
{
	public Hero HeroToTalkTo;

	public Action<bool, TextObject> IsTalkAvailable { get; private set; }

	public PartyScreenCharacterTalkPermissionEvent(Hero heroToTalkTo, Action<bool, TextObject> isTalkAvailable)
	{
		HeroToTalkTo = heroToTalkTo;
		IsTalkAvailable = isTalkAvailable;
	}
}
