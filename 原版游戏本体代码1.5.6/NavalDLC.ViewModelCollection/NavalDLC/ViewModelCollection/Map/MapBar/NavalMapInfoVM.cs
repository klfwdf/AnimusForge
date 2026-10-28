using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Map.MapBar;

public class NavalMapInfoVM : MapInfoVM
{
	private MapInfoItemVM _shipHealthInfo;

	private string _invalidShipHealthText;

	private readonly ShipHealthPercentageComparer _shipHealthPercentageComparer = new ShipHealthPercentageComparer();

	public NavalMapInfoVM()
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_invalidShipHealthText = new TextObject("{=4NaOKslb}-").ToString();
	}

	protected override void CreateItems()
	{
		base.CreateItems();
		_shipHealthInfo = new MapInfoItemVM("ship_health", GetShipTooltip);
		base.PrimaryInfoItems.Insert(2, _shipHealthInfo);
	}

	protected override void UpdatePlayerInfo(bool updateForced)
	{
		base.UpdatePlayerInfo(updateForced);
		if (MobileParty.MainParty?.Ships == null || MobileParty.MainParty.Ships.Count == 0)
		{
			_shipHealthInfo.Value = _invalidShipHealthText;
			return;
		}
		float num = MobileParty.MainParty.Ships.Average((Ship s) => s.GetHealthPercent());
		_shipHealthInfo.HasWarning = num < 20f;
		if (_shipHealthInfo.FloatValue != num)
		{
			_shipHealthInfo.Value = GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", MathF.Ceiling(num).ToString()).ToString();
		}
	}

	private List<TooltipProperty> GetShipTooltip()
	{
		if (MobileParty.MainParty?.Ships == null || MobileParty.MainParty.Ships.Count == 0)
		{
			return new List<TooltipProperty>
			{
				new TooltipProperty("", new TextObject("{=lb2hbQyx}You don't have any ships").ToString(), 0)
			};
		}
		List<TooltipProperty> list = new List<TooltipProperty>();
		float f = MobileParty.MainParty.Ships.Average((Ship s) => s.GetHealthPercent());
		list.Add(new TooltipProperty(new TextObject("{=oTM78wf6}Fleet Condition").ToString(), GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", MathF.Ceiling(f).ToString()).ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		List<Ship> list2 = MobileParty.MainParty.Ships.ToList();
		list2.Sort(_shipHealthPercentageComparer);
		foreach (Ship item in list2)
		{
			string value = GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", MathF.Ceiling(item.GetHealthPercent()).ToString()).ToString();
			list.Add(new TooltipProperty(item.Name.ToString(), value, 0));
		}
		return list;
	}
}
