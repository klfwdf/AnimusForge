using System;
using System.Reflection;

namespace System.Runtime.Serialization
{
	// Token: 0x02000738 RID: 1848
	[Serializable]
	internal class MemberHolder
	{
		// Token: 0x060051C7 RID: 20935 RVA: 0x0011FDDD File Offset: 0x0011DFDD
		internal MemberHolder(Type type, StreamingContext ctx)
		{
			this.memberType = type;
			this.context = ctx;
		}

		// Token: 0x060051C8 RID: 20936 RVA: 0x0011FDF3 File Offset: 0x0011DFF3
		public override int GetHashCode()
		{
			return this.memberType.GetHashCode();
		}

		// Token: 0x060051C9 RID: 20937 RVA: 0x0011FE00 File Offset: 0x0011E000
		public override bool Equals(object obj)
		{
			if (!(obj is MemberHolder))
			{
				return false;
			}
			MemberHolder memberHolder = (MemberHolder)obj;
			return memberHolder.memberType == this.memberType && memberHolder.context.State == this.context.State;
		}

		// Token: 0x04002441 RID: 9281
		internal MemberInfo[] members;

		// Token: 0x04002442 RID: 9282
		internal Type memberType;

		// Token: 0x04002443 RID: 9283
		internal StreamingContext context;
	}
}
