using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005DD RID: 1501
	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	internal struct CustomAttributeType
	{
		// Token: 0x06004570 RID: 17776 RVA: 0x000FF609 File Offset: 0x000FD809
		public CustomAttributeType(CustomAttributeEncoding encodedType, CustomAttributeEncoding encodedArrayType, CustomAttributeEncoding encodedEnumType, string enumName)
		{
			this.m_encodedType = encodedType;
			this.m_encodedArrayType = encodedArrayType;
			this.m_encodedEnumType = encodedEnumType;
			this.m_enumName = enumName;
			this.m_padding = this.m_encodedType;
		}

		// Token: 0x17000A59 RID: 2649
		// (get) Token: 0x06004571 RID: 17777 RVA: 0x000FF634 File Offset: 0x000FD834
		public CustomAttributeEncoding EncodedType
		{
			get
			{
				return this.m_encodedType;
			}
		}

		// Token: 0x17000A5A RID: 2650
		// (get) Token: 0x06004572 RID: 17778 RVA: 0x000FF63C File Offset: 0x000FD83C
		public CustomAttributeEncoding EncodedEnumType
		{
			get
			{
				return this.m_encodedEnumType;
			}
		}

		// Token: 0x17000A5B RID: 2651
		// (get) Token: 0x06004573 RID: 17779 RVA: 0x000FF644 File Offset: 0x000FD844
		public CustomAttributeEncoding EncodedArrayType
		{
			get
			{
				return this.m_encodedArrayType;
			}
		}

		// Token: 0x17000A5C RID: 2652
		// (get) Token: 0x06004574 RID: 17780 RVA: 0x000FF64C File Offset: 0x000FD84C
		[ComVisible(true)]
		public string EnumName
		{
			get
			{
				return this.m_enumName;
			}
		}

		// Token: 0x04001C8B RID: 7307
		private string m_enumName;

		// Token: 0x04001C8C RID: 7308
		private CustomAttributeEncoding m_encodedType;

		// Token: 0x04001C8D RID: 7309
		private CustomAttributeEncoding m_encodedEnumType;

		// Token: 0x04001C8E RID: 7310
		private CustomAttributeEncoding m_encodedArrayType;

		// Token: 0x04001C8F RID: 7311
		private CustomAttributeEncoding m_padding;
	}
}
