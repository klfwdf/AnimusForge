using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200086D RID: 2157
	[Serializable]
	internal class SerializationMonkey : ISerializable, IFieldInfo
	{
		// Token: 0x06005BC8 RID: 23496 RVA: 0x00142245 File Offset: 0x00140445
		[SecurityCritical]
		internal SerializationMonkey(SerializationInfo info, StreamingContext ctx)
		{
			this._obj.RootSetObjectData(info, ctx);
		}

		// Token: 0x06005BC9 RID: 23497 RVA: 0x0014225A File Offset: 0x0014045A
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x17000FA6 RID: 4006
		// (get) Token: 0x06005BCA RID: 23498 RVA: 0x0014226B File Offset: 0x0014046B
		// (set) Token: 0x06005BCB RID: 23499 RVA: 0x00142273 File Offset: 0x00140473
		public string[] FieldNames
		{
			[SecurityCritical]
			get
			{
				return this.fieldNames;
			}
			[SecurityCritical]
			set
			{
				this.fieldNames = value;
			}
		}

		// Token: 0x17000FA7 RID: 4007
		// (get) Token: 0x06005BCC RID: 23500 RVA: 0x0014227C File Offset: 0x0014047C
		// (set) Token: 0x06005BCD RID: 23501 RVA: 0x00142284 File Offset: 0x00140484
		public Type[] FieldTypes
		{
			[SecurityCritical]
			get
			{
				return this.fieldTypes;
			}
			[SecurityCritical]
			set
			{
				this.fieldTypes = value;
			}
		}

		// Token: 0x04002980 RID: 10624
		internal ISerializationRootObject _obj;

		// Token: 0x04002981 RID: 10625
		internal string[] fieldNames;

		// Token: 0x04002982 RID: 10626
		internal Type[] fieldTypes;
	}
}
