using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005DA RID: 1498
	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	internal struct CustomAttributeNamedParameter
	{
		// Token: 0x0600456A RID: 17770 RVA: 0x000FF5AC File Offset: 0x000FD7AC
		public CustomAttributeNamedParameter(string argumentName, CustomAttributeEncoding fieldOrProperty, CustomAttributeType type)
		{
			if (argumentName == null)
			{
				throw new ArgumentNullException("argumentName");
			}
			this.m_argumentName = argumentName;
			this.m_fieldOrProperty = fieldOrProperty;
			this.m_padding = fieldOrProperty;
			this.m_type = type;
			this.m_encodedArgument = default(CustomAttributeEncodedArgument);
		}

		// Token: 0x17000A57 RID: 2647
		// (get) Token: 0x0600456B RID: 17771 RVA: 0x000FF5E4 File Offset: 0x000FD7E4
		public CustomAttributeEncodedArgument EncodedArgument
		{
			get
			{
				return this.m_encodedArgument;
			}
		}

		// Token: 0x04001C80 RID: 7296
		private string m_argumentName;

		// Token: 0x04001C81 RID: 7297
		private CustomAttributeEncoding m_fieldOrProperty;

		// Token: 0x04001C82 RID: 7298
		private CustomAttributeEncoding m_padding;

		// Token: 0x04001C83 RID: 7299
		private CustomAttributeType m_type;

		// Token: 0x04001C84 RID: 7300
		private CustomAttributeEncodedArgument m_encodedArgument;
	}
}
