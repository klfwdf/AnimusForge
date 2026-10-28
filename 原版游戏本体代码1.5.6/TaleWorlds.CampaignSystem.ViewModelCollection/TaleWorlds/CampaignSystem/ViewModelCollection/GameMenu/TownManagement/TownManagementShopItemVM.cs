using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class TownManagementShopItemVM : ViewModel
{
	private readonly Workshop _workshop;

	private bool _isEmpty;

	private string _shopName;

	private string _shopId;

	[DataSourceProperty]
	public bool IsEmpty
	{
		get
		{
			return _isEmpty;
		}
		set
		{
			if (value != _isEmpty)
			{
				_isEmpty = value;
				OnPropertyChangedWithValue(value, "IsEmpty");
			}
		}
	}

	[DataSourceProperty]
	public string ShopName
	{
		get
		{
			return _shopName;
		}
		set
		{
			if (value != _shopName)
			{
				_shopName = value;
				OnPropertyChangedWithValue(value, "ShopName");
			}
		}
	}

	[DataSourceProperty]
	public string ShopId
	{
		get
		{
			return _shopId;
		}
		set
		{
			if (value != _shopId)
			{
				_shopId = value;
				OnPropertyChangedWithValue(value, "ShopId");
			}
		}
	}

	public TownManagementShopItemVM(Workshop workshop)
	{
		_workshop = workshop;
		IsEmpty = _workshop.WorkshopType == null;
		if (!IsEmpty)
		{
			ShopId = _workshop.WorkshopType.StringId;
		}
		else
		{
			ShopId = "empty";
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (!IsEmpty)
		{
			ShopName = _workshop.WorkshopType.Name.ToString();
		}
		else
		{
			ShopName = GameTexts.FindText("str_empty").ToString();
		}
	}

	public void ExecuteBeginHint()
	{
		if (_workshop.WorkshopType != null)
		{
			InformationManager.ShowTooltip(typeof(Workshop), _workshop);
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
