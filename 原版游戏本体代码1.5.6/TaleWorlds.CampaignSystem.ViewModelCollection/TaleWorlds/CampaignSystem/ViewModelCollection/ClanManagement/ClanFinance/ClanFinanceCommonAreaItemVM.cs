using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class ClanFinanceCommonAreaItemVM : ClanFinanceIncomeItemBaseVM
{
	private Alley _alley;

	public ClanFinanceCommonAreaItemVM(Alley alley, Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
		: base(onSelection, onRefresh)
	{
		base.IncomeTypeAsEnum = IncomeTypes.CommonArea;
		_alley = alley;
		GameTexts.SetVariable("SETTLEMENT_NAME", alley.Settlement.Name);
		GameTexts.SetVariable("COMMON_AREA_NAME", alley.Name);
		base.Name = GameTexts.FindText("str_clan_finance_common_area").ToString();
		base.Income = Campaign.Current.Models.AlleyModel.GetDailyIncomeOfAlley(alley);
		base.Visual = ((alley.Owner.CharacterObject != null) ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(alley.Owner.CharacterObject)) : new CharacterImageIdentifierVM(null));
		base.IncomeValueText = DetermineIncomeText(base.Income);
		PopulateActionList();
		PopulateStatsList();
	}

	protected override void PopulateActionList()
	{
	}

	protected override void PopulateStatsList()
	{
		base.ItemProperties.Add(new SelectableItemPropertyVM("TEST", "TEST"));
	}
}
