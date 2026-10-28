using System;

namespace MCM.Abstractions;

public interface IExternalSettingsProviderCanInvalidateCache
{
	event Action<ExternalSettingsProviderInvalidateCacheType> InstanceCacheInvalidated;
}
