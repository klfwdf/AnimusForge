using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000334 RID: 820
	[ComVisible(false)]
	public abstract class IdentityReference
	{
		// Token: 0x060028F8 RID: 10488 RVA: 0x0009714F File Offset: 0x0009534F
		internal IdentityReference()
		{
		}

		// Token: 0x17000555 RID: 1365
		// (get) Token: 0x060028F9 RID: 10489
		public abstract string Value { get; }

		// Token: 0x060028FA RID: 10490
		public abstract bool IsValidTargetType(Type targetType);

		// Token: 0x060028FB RID: 10491
		public abstract IdentityReference Translate(Type targetType);

		// Token: 0x060028FC RID: 10492
		public abstract override bool Equals(object o);

		// Token: 0x060028FD RID: 10493
		public abstract override int GetHashCode();

		// Token: 0x060028FE RID: 10494
		public abstract override string ToString();

		// Token: 0x060028FF RID: 10495 RVA: 0x00097158 File Offset: 0x00095358
		public static bool operator ==(IdentityReference left, IdentityReference right)
		{
			return (left == null && right == null) || (left != null && right != null && left.Equals(right));
		}

		// Token: 0x06002900 RID: 10496 RVA: 0x00097180 File Offset: 0x00095380
		public static bool operator !=(IdentityReference left, IdentityReference right)
		{
			return !(left == right);
		}
	}
}
