using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;

public class ClanSupporterItemVM : ViewModel
{
	private HeroVM _hero;

	[DataSourceProperty]
	public HeroVM Hero
	{
		get
		{
			return _hero;
		}
		set
		{
			if (value != _hero)
			{
				_hero = value;
				OnPropertyChangedWithValue(value, "Hero");
			}
		}
	}

	public ClanSupporterItemVM(Hero hero)
	{
		Hero = new HeroVM(hero);
	}

	public void ExecuteOpenTooltip()
	{
		InformationManager.ShowTooltip(typeof(Hero), Hero.Hero, false);
	}

	public void ExecuteCloseTooltip()
	{
		MBInformationManager.HideInformations();
	}
}
