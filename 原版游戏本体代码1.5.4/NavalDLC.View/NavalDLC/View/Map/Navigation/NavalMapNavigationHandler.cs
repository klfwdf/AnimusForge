using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;

namespace NavalDLC.View.Map.Navigation;

public class NavalMapNavigationHandler : MapNavigationHandler
{
	public ManageFleetNavigationElement ManageFleetNavigationElement { get; private set; }

	protected override INavigationElement[] OnCreateElements()
	{
		ManageFleetNavigationElement = new ManageFleetNavigationElement(this);
		List<INavigationElement> list = base.OnCreateElements().ToList();
		list.Insert(3, ManageFleetNavigationElement);
		return list.ToArray();
	}
}
