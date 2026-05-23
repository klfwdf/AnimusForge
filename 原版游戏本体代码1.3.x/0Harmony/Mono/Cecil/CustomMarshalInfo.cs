using System;

namespace Mono.Cecil
{
	// Token: 0x0200025E RID: 606
	internal sealed class CustomMarshalInfo : MarshalInfo
	{
		// Token: 0x17000328 RID: 808
		// (get) Token: 0x06000DB0 RID: 3504 RVA: 0x0002D6EA File Offset: 0x0002B8EA
		// (set) Token: 0x06000DB1 RID: 3505 RVA: 0x0002D6F2 File Offset: 0x0002B8F2
		public Guid Guid
		{
			get
			{
				return this.guid;
			}
			set
			{
				this.guid = value;
			}
		}

		// Token: 0x17000329 RID: 809
		// (get) Token: 0x06000DB2 RID: 3506 RVA: 0x0002D6FB File Offset: 0x0002B8FB
		// (set) Token: 0x06000DB3 RID: 3507 RVA: 0x0002D703 File Offset: 0x0002B903
		public string UnmanagedType
		{
			get
			{
				return this.unmanaged_type;
			}
			set
			{
				this.unmanaged_type = value;
			}
		}

		// Token: 0x1700032A RID: 810
		// (get) Token: 0x06000DB4 RID: 3508 RVA: 0x0002D70C File Offset: 0x0002B90C
		// (set) Token: 0x06000DB5 RID: 3509 RVA: 0x0002D714 File Offset: 0x0002B914
		public TypeReference ManagedType
		{
			get
			{
				return this.managed_type;
			}
			set
			{
				this.managed_type = value;
			}
		}

		// Token: 0x1700032B RID: 811
		// (get) Token: 0x06000DB6 RID: 3510 RVA: 0x0002D71D File Offset: 0x0002B91D
		// (set) Token: 0x06000DB7 RID: 3511 RVA: 0x0002D725 File Offset: 0x0002B925
		public string Cookie
		{
			get
			{
				return this.cookie;
			}
			set
			{
				this.cookie = value;
			}
		}

		// Token: 0x06000DB8 RID: 3512 RVA: 0x0002D72E File Offset: 0x0002B92E
		public CustomMarshalInfo()
			: base(NativeType.CustomMarshaler)
		{
		}

		// Token: 0x0400040E RID: 1038
		internal Guid guid;

		// Token: 0x0400040F RID: 1039
		internal string unmanaged_type;

		// Token: 0x04000410 RID: 1040
		internal TypeReference managed_type;

		// Token: 0x04000411 RID: 1041
		internal string cookie;
	}
}
