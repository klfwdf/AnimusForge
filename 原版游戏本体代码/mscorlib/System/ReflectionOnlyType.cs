using System;

namespace System
{
	// Token: 0x0200012D RID: 301
	[Serializable]
	internal class ReflectionOnlyType : RuntimeType
	{
		// Token: 0x060011AF RID: 4527 RVA: 0x00036A0D File Offset: 0x00034C0D
		private ReflectionOnlyType()
		{
		}

		// Token: 0x170001EB RID: 491
		// (get) Token: 0x060011B0 RID: 4528 RVA: 0x00036A15 File Offset: 0x00034C15
		public override RuntimeTypeHandle TypeHandle
		{
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
			}
		}
	}
}
