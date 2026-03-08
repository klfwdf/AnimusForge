using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000134 RID: 308
	public class ClanFinanceMercenaryItemVM : ClanFinanceIncomeItemBaseVM
	{
		// Token: 0x170009B7 RID: 2487
		// (get) Token: 0x06001C95 RID: 7317 RVA: 0x00069997 File Offset: 0x00067B97
		// (set) Token: 0x06001C96 RID: 7318 RVA: 0x0006999F File Offset: 0x00067B9F
		public Clan Clan { get; private set; }

		// Token: 0x06001C97 RID: 7319 RVA: 0x000699A8 File Offset: 0x00067BA8
		public ClanFinanceMercenaryItemVM(Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
			: base(onSelection, onRefresh)
		{
			base.IncomeTypeAsEnum = IncomeTypes.MercenaryService;
			this.Clan = Clan.PlayerClan;
			if (this.Clan.IsUnderMercenaryService)
			{
				base.Name = GameTexts.FindText("str_mercenary_service", null).ToString();
				base.Income = (int)(this.Clan.Influence * (float)this.Clan.MercenaryAwardMultiplier);
				base.Visual = new BannerImageIdentifierVM(this.Clan.Banner, false);
				base.IncomeValueText = base.DetermineIncomeText(base.Income);
			}
		}

		// Token: 0x06001C98 RID: 7320 RVA: 0x00069A3A File Offset: 0x00067C3A
		protected override void PopulateStatsList()
		{
			base.ItemProperties.Add(new SelectableItemPropertyVM("TEST", "TEST", false, null));
		}

		// Token: 0x06001C99 RID: 7321 RVA: 0x00069A58 File Offset: 0x00067C58
		protected override void PopulateActionList()
		{
		}
	}
}
