using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade.Options.ManagedOptions;

public class ManagedBooleanOptionData : ManagedOptionData, IBooleanOptionData, IOptionData
{
	public ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type)
		: base(type)
	{
	}
}
