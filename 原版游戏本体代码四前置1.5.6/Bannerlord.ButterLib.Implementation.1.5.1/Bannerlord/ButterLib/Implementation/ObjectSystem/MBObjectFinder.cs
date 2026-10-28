using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.ButterLib.ObjectSystem;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem;

internal class MBObjectFinder : IMBObjectFinder
{
	private static readonly AccessTools.FieldRef<CampaignObjectManager, object?[]>? CampaignObjectTypeObjects = AccessTools2.FieldRefAccess<CampaignObjectManager, object[]>("_objects");

	private static readonly Type? ICampaignObjectTypeType = AccessTools2.TypeByName("TaleWorlds.CampaignSystem.CampaignObjectManager+ICampaignObjectType");

	private static readonly MethodInfo? ObjectClassGetter = AccessTools2.PropertyGetter(ICampaignObjectTypeType, "ObjectClass");

	private static MBObjectBase? FindCampaignObjectManager(MBGUID id, Type type)
	{
		AccessTools.FieldRef<CampaignObjectManager, object?[]>? campaignObjectTypeObjects = CampaignObjectTypeObjects;
		object[] array = ((campaignObjectTypeObjects != null) ? campaignObjectTypeObjects(Campaign.Current.CampaignObjectManager) : null) ?? Array.Empty<object>();
		foreach (object obj in array)
		{
			if (obj != null && ObjectClassGetter?.Invoke(obj, Array.Empty<object>()) is Type type2 && type2 == type && obj is IEnumerable<MBObjectBase> source)
			{
				MBObjectBase mBObjectBase = source.FirstOrDefault((MBObjectBase o) => o.Id == id);
				if (mBObjectBase != null)
				{
					return mBObjectBase;
				}
			}
		}
		try
		{
			return MBObjectManager.Instance.GetObject(id);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public MBObjectBase? Find(MBGUID id, Type? type = null)
	{
		return FindCampaignObjectManager(id, type ?? typeof(MBObjectBase));
	}
}
