using System;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000C5 RID: 197
	public interface IReadOnlyPropertyOwner<T> where T : MBObjectBase
	{
		// Token: 0x06000ACD RID: 2765
		int GetPropertyValue(T attribute);

		// Token: 0x06000ACE RID: 2766
		bool HasProperty(T attribute);
	}
}
