using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class ClanFinanceMercenaryItemVM : ClanFinanceIncomeItemBaseVM
{
	public Clan Clan { get; private set; }

	public ClanFinanceMercenaryItemVM(Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
		: base(onSelection, onRefresh)
	{
		base.IncomeTypeAsEnum = IncomeTypes.MercenaryService;
		Clan = Clan.PlayerClan;
		if (Clan.IsUnderMercenaryService)
		{
			base.Name = GameTexts.FindText("str_mercenary_service").ToString();
			base.Income = (int)(Clan.Influence * (float)Clan.MercenaryAwardMultiplier);
			base.Visual = new BannerImageIdentifierVM(Clan.Banner);
			base.IncomeValueText = DetermineIncomeText(base.Income);
		}
	}

	protected override void PopulateStatsList()
	{
		base.ItemProperties.Add(new SelectableItemPropertyVM("TEST", "TEST"));
	}

	protected override void PopulateActionList()
	{
	}
}
