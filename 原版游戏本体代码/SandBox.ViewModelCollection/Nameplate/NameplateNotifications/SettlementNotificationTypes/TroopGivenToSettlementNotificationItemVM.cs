using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x02000029 RID: 41
	public class TroopGivenToSettlementNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x17000112 RID: 274
		// (get) Token: 0x0600036A RID: 874 RVA: 0x0000ED6B File Offset: 0x0000CF6B
		// (set) Token: 0x0600036B RID: 875 RVA: 0x0000ED73 File Offset: 0x0000CF73
		public Hero GiverHero { get; private set; }

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x0600036C RID: 876 RVA: 0x0000ED7C File Offset: 0x0000CF7C
		// (set) Token: 0x0600036D RID: 877 RVA: 0x0000ED84 File Offset: 0x0000CF84
		public TroopRoster Troops { get; private set; }

		// Token: 0x0600036E RID: 878 RVA: 0x0000ED90 File Offset: 0x0000CF90
		public TroopGivenToSettlementNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero giverHero, TroopRoster troops, int createdTick)
			: base(onRemove, createdTick)
		{
			this.GiverHero = giverHero;
			this.Troops = troops;
			base.Text = SandBoxUIHelper.GetTroopGivenToSettlementNotificationText(this.Troops.TotalManCount);
			base.CharacterName = ((this.GiverHero != null) ? this.GiverHero.Name.ToString() : "null hero");
			base.CharacterVisual = ((this.GiverHero != null) ? new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(this.GiverHero.CharacterObject, false)) : new CharacterImageIdentifierVM(null));
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			if (this.GiverHero != null)
			{
				base.RelationType = (this.GiverHero.Clan.IsAtWarWith(Hero.MainHero.Clan) ? (-1) : 1);
			}
		}

		// Token: 0x0600036F RID: 879 RVA: 0x0000EE58 File Offset: 0x0000D058
		public void AddNewAction(TroopRoster newTroops)
		{
			this.Troops.Add(newTroops);
			base.Text = SandBoxUIHelper.GetTroopGivenToSettlementNotificationText(this.Troops.TotalManCount);
		}
	}
}
