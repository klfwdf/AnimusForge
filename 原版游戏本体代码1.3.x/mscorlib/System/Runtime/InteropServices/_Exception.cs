using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200094C RID: 2380
	[Guid("b36b5c63-42ef-38bc-a07e-0b34c98f164a")]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[CLSCompliant(false)]
	[ComVisible(true)]
	public interface _Exception
	{
		// Token: 0x060060B0 RID: 24752
		string ToString();

		// Token: 0x060060B1 RID: 24753
		bool Equals(object obj);

		// Token: 0x060060B2 RID: 24754
		int GetHashCode();

		// Token: 0x060060B3 RID: 24755
		Type GetType();

		// Token: 0x170010FD RID: 4349
		// (get) Token: 0x060060B4 RID: 24756
		string Message { get; }

		// Token: 0x060060B5 RID: 24757
		Exception GetBaseException();

		// Token: 0x170010FE RID: 4350
		// (get) Token: 0x060060B6 RID: 24758
		string StackTrace { get; }

		// Token: 0x170010FF RID: 4351
		// (get) Token: 0x060060B7 RID: 24759
		// (set) Token: 0x060060B8 RID: 24760
		string HelpLink { get; set; }

		// Token: 0x17001100 RID: 4352
		// (get) Token: 0x060060B9 RID: 24761
		// (set) Token: 0x060060BA RID: 24762
		string Source { get; set; }

		// Token: 0x060060BB RID: 24763
		[SecurityCritical]
		void GetObjectData(SerializationInfo info, StreamingContext context);

		// Token: 0x17001101 RID: 4353
		// (get) Token: 0x060060BC RID: 24764
		Exception InnerException { get; }

		// Token: 0x17001102 RID: 4354
		// (get) Token: 0x060060BD RID: 24765
		MethodBase TargetSite { get; }
	}
}
