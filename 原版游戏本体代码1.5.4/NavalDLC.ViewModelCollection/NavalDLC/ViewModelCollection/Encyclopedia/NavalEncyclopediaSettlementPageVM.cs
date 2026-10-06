using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Encyclopedia;

[EncyclopediaViewModel(typeof(Settlement))]
public class NavalEncyclopediaSettlementPageVM : EncyclopediaSettlementPageVM
{
	private string _shipyardText;

	private BasicTooltipViewModel _shipyardHint;

	[DataSourceProperty]
	public string ShipyardText
	{
		get
		{
			return _shipyardText;
		}
		set
		{
			if (value != _shipyardText)
			{
				_shipyardText = value;
				OnPropertyChangedWithValue(value, "ShipyardText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ShipyardHint
	{
		get
		{
			return _shipyardHint;
		}
		set
		{
			if (value != _shipyardHint)
			{
				_shipyardHint = value;
				OnPropertyChangedWithValue(value, "ShipyardHint");
			}
		}
	}

	public NavalEncyclopediaSettlementPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
	}

	public override void Refresh()
	{
		base.Refresh();
		if (_settlement.Town?.GetShipyard() == null)
		{
			return;
		}
		TextObject disableReason;
		bool flag = CampaignUIHelper.IsSettlementInformationHidden(_settlement, out disableReason);
		string text = GameTexts.FindText("str_missing_info_indicator").ToString();
		ShipyardText = (flag ? text : _settlement.Town?.GetShipyard()?.CurrentLevel.ToString());
		ShipyardHint = new BasicTooltipViewModel(() => NavalUIHelper.GetShipyardTooltip(_settlement.Town));
		for (int num = 0; num < base.LeftSideProperties.Count; num++)
		{
			if (base.LeftSideProperties[num].TypeString == "Wall")
			{
				EncyclopediaSettlementPageStatItemVM item = base.LeftSideProperties[base.LeftSideProperties.Count - 1];
				base.LeftSideProperties.Remove(item);
				base.RightSideProperties.Insert(0, item);
				base.LeftSideProperties.Insert(num + 1, new EncyclopediaSettlementPageStatItemVM(ShipyardHint, EncyclopediaSettlementPageStatItemVM.DescriptionType.Shipyard, ShipyardText));
				break;
			}
		}
	}
}
