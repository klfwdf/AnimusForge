using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingPerkVM : ViewModel
{
	public readonly PerkObject Perk;

	private string _name;

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

	public CraftingPerkVM(PerkObject perk)
	{
		Perk = perk;
		Name = Perk.Name.ToString();
	}
}
