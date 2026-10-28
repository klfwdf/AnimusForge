using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class ListTypeVM : ViewModel
{
	public readonly EncyclopediaPage EncyclopediaPage;

	private string _name;

	private string _id;

	private string _imageId;

	private int _order;

	[DataSourceProperty]
	public string ID
	{
		get
		{
			return _id;
		}
		set
		{
			if (value != _id)
			{
				_id = value;
				OnPropertyChangedWithValue(value, "ID");
			}
		}
	}

	[DataSourceProperty]
	public int Order
	{
		get
		{
			return _order;
		}
		set
		{
			if (value != _order)
			{
				_order = value;
				OnPropertyChangedWithValue(value, "Order");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string ImageID
	{
		get
		{
			return _imageId;
		}
		set
		{
			if (value != _imageId)
			{
				_imageId = value;
				OnPropertyChangedWithValue(value, "ImageID");
			}
		}
	}

	public ListTypeVM(EncyclopediaPage encyclopediaPage)
	{
		EncyclopediaPage = encyclopediaPage;
		ID = encyclopediaPage.GetIdentifierNames()[0];
		ImageID = encyclopediaPage.GetStringID();
		Order = encyclopediaPage.HomePageOrderIndex;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = EncyclopediaPage.GetName().ToString();
	}

	public void Execute()
	{
		Campaign.Current.EncyclopediaManager.GoToLink("ListPage", ID);
	}
}
