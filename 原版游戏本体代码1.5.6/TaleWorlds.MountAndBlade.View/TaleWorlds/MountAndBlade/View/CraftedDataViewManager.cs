using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.View;

public class CraftedDataViewManager
{
	private static Dictionary<WeaponDesign, CraftedDataView> _craftedDataViews;

	public static void Initialize()
	{
		_craftedDataViews = new Dictionary<WeaponDesign, CraftedDataView>();
	}

	public static void Clear()
	{
		foreach (CraftedDataView value in _craftedDataViews.Values)
		{
			value.Clear();
		}
		_craftedDataViews.Clear();
	}

	public static CraftedDataView GetCraftedDataView(WeaponDesign craftedData)
	{
		if (craftedData != null)
		{
			if (!_craftedDataViews.TryGetValue(craftedData, out var value))
			{
				value = new CraftedDataView(craftedData);
				_craftedDataViews.Add(craftedData, value);
			}
			return value;
		}
		return null;
	}
}
