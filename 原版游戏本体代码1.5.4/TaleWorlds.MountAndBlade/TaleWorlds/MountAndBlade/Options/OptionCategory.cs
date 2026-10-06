using System.Collections.Generic;
using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade.Options;

public class OptionCategory
{
	public readonly IEnumerable<IOptionData> BaseOptions;

	public readonly IEnumerable<OptionGroup> Groups;

	public OptionCategory(IEnumerable<IOptionData> baseOptions, IEnumerable<OptionGroup> groups)
	{
		BaseOptions = baseOptions;
		Groups = groups;
	}
}
