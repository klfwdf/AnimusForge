using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingResourceItemVM : ViewModel
{
	private string _resourceName;

	private string _resourceItemStringId;

	private int _resourceUsageAmount;

	private int _resourceChangeAmount;

	private string _resourceMaterialTypeAsStr;

	private HintViewModel _resourceHint;

	private bool _isResourceAvailable = true;

	public ItemObject ResourceItem { get; private set; }

	public CraftingMaterials ResourceMaterial { get; private set; }

	[DataSourceProperty]
	public string ResourceName
	{
		get
		{
			return _resourceName;
		}
		set
		{
			if (value != _resourceName)
			{
				_resourceName = value;
				OnPropertyChangedWithValue(value, "ResourceName");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResourceHint
	{
		get
		{
			return _resourceHint;
		}
		set
		{
			if (value != _resourceHint)
			{
				_resourceHint = value;
				OnPropertyChangedWithValue(value, "ResourceHint");
			}
		}
	}

	[DataSourceProperty]
	public string ResourceMaterialTypeAsStr
	{
		get
		{
			return _resourceMaterialTypeAsStr;
		}
		set
		{
			if (value != _resourceMaterialTypeAsStr)
			{
				_resourceMaterialTypeAsStr = value;
				OnPropertyChangedWithValue(value, "ResourceMaterialTypeAsStr");
			}
		}
	}

	[DataSourceProperty]
	public int ResourceAmount
	{
		get
		{
			return _resourceUsageAmount;
		}
		set
		{
			if (value != _resourceUsageAmount)
			{
				_resourceUsageAmount = value;
				OnPropertyChangedWithValue(value, "ResourceAmount");
			}
		}
	}

	[DataSourceProperty]
	public int ResourceChangeAmount
	{
		get
		{
			return _resourceChangeAmount;
		}
		set
		{
			if (value != _resourceChangeAmount)
			{
				_resourceChangeAmount = value;
				OnPropertyChangedWithValue(value, "ResourceChangeAmount");
			}
		}
	}

	[DataSourceProperty]
	public string ResourceItemStringId
	{
		get
		{
			return _resourceItemStringId;
		}
		set
		{
			if (value != _resourceItemStringId)
			{
				_resourceItemStringId = value;
				OnPropertyChangedWithValue(value, "ResourceItemStringId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsResourceAvailable
	{
		get
		{
			return _isResourceAvailable;
		}
		set
		{
			if (value != _isResourceAvailable)
			{
				_isResourceAvailable = value;
				OnPropertyChangedWithValue(value, "IsResourceAvailable");
			}
		}
	}

	public CraftingResourceItemVM(CraftingMaterials material, int amount, int changeAmount = 0)
	{
		ResourceMaterial = material;
		ResourceItem = Campaign.Current?.Models?.SmithingModel?.GetCraftingMaterialItem(material);
		ResourceName = ResourceItem?.Name?.ToString() ?? "none";
		ResourceHint = new HintViewModel(new TextObject("{=!}" + ResourceName));
		ResourceAmount = amount;
		ResourceItemStringId = ResourceItem?.StringId ?? "none";
		ResourceMaterialTypeAsStr = ResourceMaterial.ToString();
		ResourceChangeAmount = changeAmount;
	}
}
