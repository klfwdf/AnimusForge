using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000650 RID: 1616
	internal sealed class InternalModuleBuilder : RuntimeModule
	{
		// Token: 0x06004C09 RID: 19465 RVA: 0x00113269 File Offset: 0x00111469
		private InternalModuleBuilder()
		{
		}

		// Token: 0x06004C0A RID: 19466 RVA: 0x00113271 File Offset: 0x00111471
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is InternalModuleBuilder)
			{
				return this == obj;
			}
			return obj.Equals(this);
		}

		// Token: 0x06004C0B RID: 19467 RVA: 0x0011328C File Offset: 0x0011148C
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
