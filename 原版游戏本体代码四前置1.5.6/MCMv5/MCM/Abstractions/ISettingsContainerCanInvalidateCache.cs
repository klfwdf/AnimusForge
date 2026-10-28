using System;

namespace MCM.Abstractions;

public interface ISettingsContainerCanInvalidateCache
{
	event Action InstanceCacheInvalidated;
}
