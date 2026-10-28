namespace TaleWorlds.Engine.Options;

public class NativeBooleanOptionData : NativeOptionData, IBooleanOptionData, IOptionData
{
	public NativeBooleanOptionData(NativeOptions.NativeOptionsType type)
		: base(type)
	{
	}
}
