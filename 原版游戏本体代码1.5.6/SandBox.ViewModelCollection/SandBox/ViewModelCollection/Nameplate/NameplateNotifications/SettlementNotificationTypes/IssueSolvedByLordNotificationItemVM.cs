using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class IssueSolvedByLordNotificationItemVM : SettlementNotificationItemBaseVM
{
	public IssueSolvedByLordNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero hero, int createdTick)
		: base(onRemove, createdTick)
	{
		base.Text = new TextObject("{=TFJTOYea}Solved an issue").ToString();
		base.CharacterName = hero?.Name?.ToString() ?? "";
		base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(hero.CharacterObject));
		base.RelationType = 0;
		base.CreatedTick = createdTick;
		base.RelationType = ((hero == null || hero.Clan?.IsAtWarWith(Hero.MainHero.Clan) != true) ? 1 : (-1));
	}
}
