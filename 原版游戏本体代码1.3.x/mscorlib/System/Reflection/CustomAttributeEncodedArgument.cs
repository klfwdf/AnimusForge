using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005D9 RID: 1497
	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	internal struct CustomAttributeEncodedArgument
	{
		// Token: 0x06004564 RID: 17764
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ParseAttributeArguments(IntPtr pCa, int cCa, ref CustomAttributeCtorParameter[] CustomAttributeCtorParameters, ref CustomAttributeNamedParameter[] CustomAttributeTypedArgument, RuntimeAssembly assembly);

		// Token: 0x06004565 RID: 17765 RVA: 0x000FF54C File Offset: 0x000FD74C
		[SecurityCritical]
		internal static void ParseAttributeArguments(ConstArray attributeBlob, ref CustomAttributeCtorParameter[] customAttributeCtorParameters, ref CustomAttributeNamedParameter[] customAttributeNamedParameters, RuntimeModule customAttributeModule)
		{
			if (customAttributeModule == null)
			{
				throw new ArgumentNullException("customAttributeModule");
			}
			if (customAttributeCtorParameters.Length != 0 || customAttributeNamedParameters.Length != 0)
			{
				CustomAttributeEncodedArgument.ParseAttributeArguments(attributeBlob.Signature, attributeBlob.Length, ref customAttributeCtorParameters, ref customAttributeNamedParameters, (RuntimeAssembly)customAttributeModule.Assembly);
			}
		}

		// Token: 0x17000A53 RID: 2643
		// (get) Token: 0x06004566 RID: 17766 RVA: 0x000FF58C File Offset: 0x000FD78C
		public CustomAttributeType CustomAttributeType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000A54 RID: 2644
		// (get) Token: 0x06004567 RID: 17767 RVA: 0x000FF594 File Offset: 0x000FD794
		public long PrimitiveValue
		{
			get
			{
				return this.m_primitiveValue;
			}
		}

		// Token: 0x17000A55 RID: 2645
		// (get) Token: 0x06004568 RID: 17768 RVA: 0x000FF59C File Offset: 0x000FD79C
		public CustomAttributeEncodedArgument[] ArrayValue
		{
			get
			{
				return this.m_arrayValue;
			}
		}

		// Token: 0x17000A56 RID: 2646
		// (get) Token: 0x06004569 RID: 17769 RVA: 0x000FF5A4 File Offset: 0x000FD7A4
		public string StringValue
		{
			get
			{
				return this.m_stringValue;
			}
		}

		// Token: 0x04001C7C RID: 7292
		private long m_primitiveValue;

		// Token: 0x04001C7D RID: 7293
		private CustomAttributeEncodedArgument[] m_arrayValue;

		// Token: 0x04001C7E RID: 7294
		private string m_stringValue;

		// Token: 0x04001C7F RID: 7295
		private CustomAttributeType m_type;
	}
}
