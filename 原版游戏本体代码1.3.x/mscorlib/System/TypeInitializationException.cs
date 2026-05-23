using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x0200014E RID: 334
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class TypeInitializationException : SystemException
	{
		// Token: 0x060014D2 RID: 5330 RVA: 0x0003DBAB File Offset: 0x0003BDAB
		private TypeInitializationException()
			: base(Environment.GetResourceString("TypeInitialization_Default"))
		{
			base.SetErrorCode(-2146233036);
		}

		// Token: 0x060014D3 RID: 5331 RVA: 0x0003DBC8 File Offset: 0x0003BDC8
		private TypeInitializationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233036);
		}

		// Token: 0x060014D4 RID: 5332 RVA: 0x0003DBDC File Offset: 0x0003BDDC
		[__DynamicallyInvokable]
		public TypeInitializationException(string fullTypeName, Exception innerException)
			: base(Environment.GetResourceString("TypeInitialization_Type", new object[] { fullTypeName }), innerException)
		{
			this._typeName = fullTypeName;
			base.SetErrorCode(-2146233036);
		}

		// Token: 0x060014D5 RID: 5333 RVA: 0x0003DC0B File Offset: 0x0003BE0B
		internal TypeInitializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._typeName = info.GetString("TypeName");
		}

		// Token: 0x1700025C RID: 604
		// (get) Token: 0x060014D6 RID: 5334 RVA: 0x0003DC26 File Offset: 0x0003BE26
		[__DynamicallyInvokable]
		public string TypeName
		{
			[__DynamicallyInvokable]
			get
			{
				if (this._typeName == null)
				{
					return string.Empty;
				}
				return this._typeName;
			}
		}

		// Token: 0x060014D7 RID: 5335 RVA: 0x0003DC3C File Offset: 0x0003BE3C
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("TypeName", this.TypeName, typeof(string));
		}

		// Token: 0x040006EC RID: 1772
		private string _typeName;
	}
}
