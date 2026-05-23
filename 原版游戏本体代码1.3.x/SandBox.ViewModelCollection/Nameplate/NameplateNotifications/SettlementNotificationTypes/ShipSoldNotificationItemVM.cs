using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes
{
	// Token: 0x02000028 RID: 40
	public class ShipSoldNotificationItemVM : SettlementNotificationItemBaseVM
	{
		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000365 RID: 869 RVA: 0x0000EBD9 File Offset: 0x0000CDD9
		public Ship Ship { get; }

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000366 RID: 870 RVA: 0x0000EBE1 File Offset: 0x0000CDE1
		public PartyBase SettlementParty { get; }

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000367 RID: 871 RVA: 0x0000EBE9 File Offset: 0x0000CDE9
		public PartyBase HeroParty { get; }

		// Token: 0x06000368 RID: 872 RVA: 0x0000EBF4 File Offset: 0x0000CDF4
		public ShipSoldNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Ship ship, PartyBase settlementParty, PartyBase heroParty, int amount, int createdTick)
			: base(onRemove, createdTick)
		{
			this.Ship = ship;
			this.SettlementParty = settlementParty;
			this.HeroParty = heroParty;
			this._amount = amount;
			base.Text = SandBoxUIHelper.GetShipSoldNotificationText(this.Ship, Math.Abs(this._amount), this._amount < 0);
			Hero leaderHero = this.HeroParty.LeaderHero;
			base.CharacterName = ((leaderHero != null) ? leaderHero.Name.ToString() : null) ?? this.HeroParty.Name.ToString();
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(this.HeroParty);
			if (visualPartyLeader != null)
			{
				base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(visualPartyLeader, false));
			}
			else if (this.HeroParty.Owner != null)
			{
				base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(this.HeroParty.Owner.CharacterObject, false));
			}
			base.RelationType = 0;
			base.CreatedTick = createdTick;
			if (this.HeroParty.LeaderHero != null)
			{
				base.RelationType = (this.HeroParty.LeaderHero.Clan.IsAtWarWith(Hero.MainHero.Clan) ? (-1) : 1);
			}
		}

		// Token: 0x06000369 RID: 873 RVA: 0x0000ED1C File Offset: 0x0000CF1C
		public void AddNewTransaction(int amount)
		{
			this._amount += amount;
			if (this._amount == 0)
			{
				base.ExecuteRemove();
				return;
			}
			base.Text = SandBoxUIHelper.GetShipSoldNotificationText(this.Ship, Math.Abs(this._amount), this._amount < 0);
		}

		// Token: 0x040001BB RID: 443
		private int _amount;
	}
}
