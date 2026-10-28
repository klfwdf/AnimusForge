using System.Collections.Generic;
using TaleWorlds.Engine.Options;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Options;

public class OptionGroup
{
	public readonly TextObject GroupName;

	public readonly IEnumerable<IOptionData> Options;

	public OptionGroup(TextObject groupName, IEnumerable<IOptionData> options)
	{
		GroupName = groupName;
		Options = options;
	}
}
