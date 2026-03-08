using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008E8 RID: 2280
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class WeakReferenceComparer : EqualityComparer<WeakReference>
	{
		// Token: 0x06002F7B RID: 12155 RVA: 0x000A5398 File Offset: 0x000A3598
		[NullableContext(2)]
		public override bool Equals(WeakReference x, WeakReference y)
		{
			if (((x != null) ? x.SafeGetTarget() : null) == ((y != null) ? y.SafeGetTarget() : null))
			{
				bool? flag = ((x != null) ? new bool?(x.SafeGetIsAlive()) : null);
				bool? flag2 = ((y != null) ? new bool?(y.SafeGetIsAlive()) : null);
				return (flag.GetValueOrDefault() == flag2.GetValueOrDefault()) & (flag != null == (flag2 != null));
			}
			return false;
		}

		// Token: 0x06002F7C RID: 12156 RVA: 0x000A5416 File Offset: 0x000A3616
		public override int GetHashCode(WeakReference obj)
		{
			object obj2 = obj.SafeGetTarget();
			if (obj2 == null)
			{
				return 0;
			}
			return obj2.GetHashCode();
		}
	}
}
