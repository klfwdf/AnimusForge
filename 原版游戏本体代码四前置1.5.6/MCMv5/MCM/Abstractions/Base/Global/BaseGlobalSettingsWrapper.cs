using System;
using MCM.Common;

namespace MCM.Abstractions.Base.Global;

[Obsolete("Will be removed from future API", true)]
public abstract class BaseGlobalSettingsWrapper : GlobalSettings, IWrapper
{
	public object Object { get; protected set; }

	protected BaseGlobalSettingsWrapper(object @object)
	{
		Object = @object;
	}
}
