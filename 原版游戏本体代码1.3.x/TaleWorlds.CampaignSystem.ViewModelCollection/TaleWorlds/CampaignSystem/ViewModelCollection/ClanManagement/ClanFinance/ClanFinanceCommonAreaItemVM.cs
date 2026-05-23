using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000133 RID: 307
	public class ClanFinanceCommonAreaItemVM : ClanFinanceIncomeItemBaseVM
	{
		// Token: 0x06001C92 RID: 7314 RVA: 0x000698B0 File Offset: 0x00067AB0
		public ClanFinanceCommonAreaItemVM(Alley alley, Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
			: base(onSelection, onRefresh)
		{
			base.IncomeTypeAsEnum = IncomeTypes.CommonArea;
			this._alley = alley;
			GameTexts.SetVariable("SETTLEMENT_NAME", alley.Settlement.Name);
			GameTexts.SetVariable("COMMON_AREA_NAME", alley.Name);
			base.Name = GameTexts.FindText("str_clan_finance_common_area", null).ToString();
			base.Income = Campaign.Current.Models.AlleyModel.GetDailyIncomeOfAlley(alley);
			base.Visual = ((alley.Owner.CharacterObject != null) ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(alley.Owner.CharacterObject)) : new CharacterImageIdentifierVM(null));
			base.IncomeValueText = base.DetermineIncomeText(base.Income);
			this.PopulateActionList();
			this.PopulateStatsList();
		}

		// Token: 0x06001C93 RID: 7315 RVA: 0x00069977 File Offset: 0x00067B77
		protected override void PopulateActionList()
		{
		}

		// Token: 0x06001C94 RID: 7316 RVA: 0x00069979 File Offset: 0x00067B79
		protected override void PopulateStatsList()
		{
			base.ItemProperties.Add(new SelectableItemPropertyVM("TEST", "TEST", false, null));
		}

		// Token: 0x04000D55 RID: 3413
		private Alley _alley;
	}
}
