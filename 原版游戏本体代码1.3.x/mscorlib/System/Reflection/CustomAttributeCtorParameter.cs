using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005DB RID: 1499
	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	internal struct CustomAttributeCtorParameter
	{
		// Token: 0x0600456C RID: 17772 RVA: 0x000FF5EC File Offset: 0x000FD7EC
		public CustomAttributeCtorParameter(CustomAttributeType type)
		{
			this.m_type = type;
			this.m_encodedArgument = default(CustomAttributeEncodedArgument);
		}

		// Token: 0x17000A58 RID: 2648
		// (get) Token: 0x0600456D RID: 17773 RVA: 0x000FF601 File Offset: 0x000FD801
		public CustomAttributeEncodedArgument CustomAttributeEncodedArgument
		{
			get
			{
				return this.m_encodedArgument;
			}
		}

		// Token: 0x04001C85 RID: 7301
		private CustomAttributeType m_type;

		// Token: 0x04001C86 RID: 7302
		private CustomAttributeEncodedArgument m_encodedArgument;
	}
}
