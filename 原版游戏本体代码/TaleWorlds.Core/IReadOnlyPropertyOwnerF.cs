using System;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000C6 RID: 198
	public interface IReadOnlyPropertyOwnerF<T> where T : MBObjectBase
	{
		// Token: 0x06000ACF RID: 2767
		float GetPropertyValue(T attribute);

		// Token: 0x06000AD0 RID: 2768
		bool HasProperty(T attribute);
	}
}
