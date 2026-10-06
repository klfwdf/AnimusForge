using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public static class VisualOrderFactory
{
	private static List<VisualOrderProvider> _providers;

	static VisualOrderFactory()
	{
		_providers = new List<VisualOrderProvider>();
	}

	public static void RegisterProvider(VisualOrderProvider provider)
	{
		if (_providers.Contains(provider) || _providers.Any((VisualOrderProvider p) => p.GetType() == provider.GetType()))
		{
			Debug.FailedAssert("Provider of type already registered: " + provider.GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderFactory.cs", "RegisterProvider", 22);
		}
		else
		{
			_providers.Add(provider);
		}
	}

	public static void UnregisterProvider(VisualOrderProvider provider)
	{
		if (!_providers.Contains(provider))
		{
			Debug.FailedAssert("Provider of type was not registered: " + provider.GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderFactory.cs", "UnregisterProvider", 33);
		}
		else
		{
			_providers.Remove(provider);
		}
	}

	public static MBReadOnlyList<VisualOrderSet> GetOrders()
	{
		for (int num = _providers.Count - 1; num >= 0; num--)
		{
			VisualOrderProvider visualOrderProvider = _providers[num];
			if (visualOrderProvider != null && visualOrderProvider.IsAvailable())
			{
				return visualOrderProvider.GetOrders();
			}
		}
		MBList<Formation> formationsIncludingEmpty = Mission.Current.PlayerTeam.FormationsIncludingEmpty;
		for (int i = 0; i < formationsIncludingEmpty.Count; i++)
		{
			if (formationsIncludingEmpty[i].CountOfUnits > 0)
			{
				Debug.FailedAssert("There are troops in formations but none of the order providers are available!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderFactory.cs", "GetOrders", 56);
				break;
			}
		}
		return new MBList<VisualOrderSet>();
	}
}
