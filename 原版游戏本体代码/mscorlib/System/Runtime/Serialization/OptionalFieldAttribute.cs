using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000739 RID: 1849
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	[ComVisible(true)]
	public sealed class OptionalFieldAttribute : Attribute
	{
		// Token: 0x17000D74 RID: 3444
		// (get) Token: 0x060051CB RID: 20939 RVA: 0x0011FE56 File Offset: 0x0011E056
		// (set) Token: 0x060051CC RID: 20940 RVA: 0x0011FE5E File Offset: 0x0011E05E
		public int VersionAdded
		{
			get
			{
				return this.versionAdded;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentException(Environment.GetResourceString("Serialization_OptionalFieldVersionValue"));
				}
				this.versionAdded = value;
			}
		}

		// Token: 0x04002444 RID: 9284
		private int versionAdded = 1;
	}
}
