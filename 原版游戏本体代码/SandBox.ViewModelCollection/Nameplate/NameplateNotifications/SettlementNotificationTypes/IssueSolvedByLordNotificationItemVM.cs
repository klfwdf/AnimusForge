using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x02000024 RID: 36
	public class IssueSolvedByLordNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x06000349 RID: 841 RVA: 0x0000E198 File Offset: 0x0000C398
		public IssueSolvedByLordNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero hero, int createdTick)
			: base(onRemove, createdTick)
		{
			base.Text = new TextObject("{=TFJTOYea}Solved an issue", null).ToString();
			string text;
			if (hero == null)
			{
				text = null;
			}
			else
			{
				TextObject name = hero.Name;
				text = ((name != null) ? name.ToString() : null);
			}
			base.CharacterName = text ?? "";
			base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(hero.CharacterObject, false));
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			int relationType;
			if (hero != null)
			{
				Clan clan = hero.Clan;
				bool? flag = ((clan != null) ? new bool?(clan.IsAtWarWith(Hero.MainHero.Clan)) : null);
				bool flag2 = true;
				if ((flag.GetValueOrDefault() == flag2) & (flag != null))
				{
					relationType = -1;
					goto IL_B2;
				}
			}
			relationType = 1;
			IL_B2:
			base.RelationType = relationType;
		}
	}
}
