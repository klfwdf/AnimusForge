using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.MountAndBlade.Diamond;

public static class ModuleInfoModelExtensions
{
	public static bool IsCompatibleWith(this IEnumerable<ModuleInfoModel> a, IEnumerable<ModuleInfoModel> b, bool allowOptionalModules)
	{
		bool num = (from m in a
			where !m.IsOptional
			orderby m.Id
			select m).SequenceEqual(from m in b
			where !m.IsOptional
			orderby m.Id
			select m);
		bool flag = a.Any((ModuleInfoModel m) => m.IsOptional) || b.Any((ModuleInfoModel m) => m.IsOptional);
		if (num)
		{
			if (!allowOptionalModules)
			{
				return !flag;
			}
			return true;
		}
		return false;
	}
}
