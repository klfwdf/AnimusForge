using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x02000025 RID: 37
	public class ItemSoldNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x17000109 RID: 265
		// (get) Token: 0x0600034A RID: 842 RVA: 0x0000E25C File Offset: 0x0000C45C
		public ItemRosterElement Item { get; }

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x0600034B RID: 843 RVA: 0x0000E264 File Offset: 0x0000C464
		public PartyBase ReceiverParty { get; }

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x0600034C RID: 844 RVA: 0x0000E26C File Offset: 0x0000C46C
		public PartyBase PayerParty { get; }

		// Token: 0x0600034D RID: 845 RVA: 0x0000E274 File Offset: 0x0000C474
		public ItemSoldNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, PartyBase receiverParty, PartyBase payerParty, ItemRosterElement item, int number, int createdTick)
			: base(onRemove, createdTick)
		{
			this.Item = item;
			this.ReceiverParty = receiverParty;
			this.PayerParty = payerParty;
			this._number = number;
			this._heroParty = (receiverParty.IsSettlement ? payerParty : receiverParty);
			base.Text = SandBoxUIHelper.GetItemSoldNotificationText(this.Item, this._number, this._number < 0);
			base.CharacterName = ((this._heroParty.LeaderHero != null) ? this._heroParty.LeaderHero.Name.ToString() : this._heroParty.Name.ToString());
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(this._heroParty);
			base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(visualPartyLeader, false));
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			if (this._heroParty.LeaderHero != null)
			{
				base.RelationType = (this._heroParty.LeaderHero.Clan.IsAtWarWith(Hero.MainHero.Clan) ? (-1) : 1);
			}
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0000E379 File Offset: 0x0000C579
		public void AddNewTransaction(int amount)
		{
			this._number += amount;
			if (this._number == 0)
			{
				base.ExecuteRemove();
				return;
			}
			base.Text = SandBoxUIHelper.GetItemSoldNotificationText(this.Item, this._number, this._number < 0);
		}

		// Token: 0x040001AF RID: 431
		private int _number;

		// Token: 0x040001B0 RID: 432
		private PartyBase _heroParty;
	}
}
