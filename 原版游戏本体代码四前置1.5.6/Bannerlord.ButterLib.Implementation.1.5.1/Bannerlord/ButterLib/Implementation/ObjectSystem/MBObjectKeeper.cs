using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem;

internal class MBObjectKeeper : IMBObjectKeeper
{
	private readonly Dictionary<MBGUID, MBObjectBase> _references = new Dictionary<MBGUID, MBObjectBase>();

	public void Keep(MBObjectBase mbObject)
	{
		if (mbObject != null)
		{
			_references[mbObject.Id] = mbObject;
		}
	}

	public void Sync(IDataStore dataStore)
	{
		if (dataStore.IsSaving)
		{
			List<MBObjectBase> data = _references.Values.ToList();
			dataStore.SyncData("keepReferences", ref data);
		}
	}
}
