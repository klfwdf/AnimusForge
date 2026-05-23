using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000067 RID: 103
	[NullableContext(2)]
	[Nullable(0)]
	internal class ReflectionMember
	{
		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000590 RID: 1424 RVA: 0x00017E4E File Offset: 0x0001604E
		// (set) Token: 0x06000591 RID: 1425 RVA: 0x00017E56 File Offset: 0x00016056
		public Type MemberType { get; set; }

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000592 RID: 1426 RVA: 0x00017E5F File Offset: 0x0001605F
		// (set) Token: 0x06000593 RID: 1427 RVA: 0x00017E67 File Offset: 0x00016067
		[Nullable(new byte[] { 2, 1, 2 })]
		public Func<object, object> Getter
		{
			[return: Nullable(new byte[] { 2, 1, 2 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 2 })]
			set;
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000594 RID: 1428 RVA: 0x00017E70 File Offset: 0x00016070
		// (set) Token: 0x06000595 RID: 1429 RVA: 0x00017E78 File Offset: 0x00016078
		[Nullable(new byte[] { 2, 1, 2 })]
		public Action<object, object> Setter
		{
			[return: Nullable(new byte[] { 2, 1, 2 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 2 })]
			set;
		}
	}
}
