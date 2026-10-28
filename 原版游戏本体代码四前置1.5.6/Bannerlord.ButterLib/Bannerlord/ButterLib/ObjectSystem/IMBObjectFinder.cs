using System;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.ObjectSystem;

public interface IMBObjectFinder
{
	MBObjectBase? Find(MBGUID id, Type? type = null);
}
