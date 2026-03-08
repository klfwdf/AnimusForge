using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x0200002A RID: 42
	public class TroopRecruitmentNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x17000114 RID: 276
		// (get) Token: 0x06000370 RID: 880 RVA: 0x0000EE7C File Offset: 0x0000D07C
		// (set) Token: 0x06000371 RID: 881 RVA: 0x0000EE84 File Offset: 0x0000D084
		public Hero RecruiterHero { get; private set; }

		// Token: 0x06000372 RID: 882 RVA: 0x0000EE90 File Offset: 0x0000D090
		public TroopRecruitmentNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero recruiterHero, int amount, int createdTick)
			: base(onRemove, createdTick)
		{
			base.Text = SandBoxUIHelper.GetRecruitNotificationText(amount);
			this._recruitAmount = amount;
			this.RecruiterHero = recruiterHero;
			base.CharacterName = ((recruiterHero != null) ? recruiterHero.Name.ToString() : "null hero");
			base.CharacterVisual = ((recruiterHero != null) ? new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(recruiterHero.CharacterObject, false)) : new CharacterImageIdentifierVM(null));
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			if (recruiterHero != null)
			{
				base.RelationType = (recruiterHero.Clan.IsAtWarWith(Hero.MainHero.Clan) ? (-1) : 1);
			}
		}

		// Token: 0x06000373 RID: 883 RVA: 0x0000EF30 File Offset: 0x0000D130
		public void AddNewAction(int addedAmount)
		{
			this._recruitAmount += addedAmount;
			base.Text = SandBoxUIHelper.GetRecruitNotificationText(this._recruitAmount);
		}

		// Token: 0x040001BE RID: 446
		private int _recruitAmount;
	}
}
