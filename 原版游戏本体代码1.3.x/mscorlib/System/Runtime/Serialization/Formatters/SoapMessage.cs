using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000765 RID: 1893
	[ComVisible(true)]
	[Serializable]
	public class SoapMessage : ISoapMessage
	{
		// Token: 0x17000DC0 RID: 3520
		// (get) Token: 0x06005315 RID: 21269 RVA: 0x00123D62 File Offset: 0x00121F62
		// (set) Token: 0x06005316 RID: 21270 RVA: 0x00123D6A File Offset: 0x00121F6A
		public string[] ParamNames
		{
			get
			{
				return this.paramNames;
			}
			set
			{
				this.paramNames = value;
			}
		}

		// Token: 0x17000DC1 RID: 3521
		// (get) Token: 0x06005317 RID: 21271 RVA: 0x00123D73 File Offset: 0x00121F73
		// (set) Token: 0x06005318 RID: 21272 RVA: 0x00123D7B File Offset: 0x00121F7B
		public object[] ParamValues
		{
			get
			{
				return this.paramValues;
			}
			set
			{
				this.paramValues = value;
			}
		}

		// Token: 0x17000DC2 RID: 3522
		// (get) Token: 0x06005319 RID: 21273 RVA: 0x00123D84 File Offset: 0x00121F84
		// (set) Token: 0x0600531A RID: 21274 RVA: 0x00123D8C File Offset: 0x00121F8C
		public Type[] ParamTypes
		{
			get
			{
				return this.paramTypes;
			}
			set
			{
				this.paramTypes = value;
			}
		}

		// Token: 0x17000DC3 RID: 3523
		// (get) Token: 0x0600531B RID: 21275 RVA: 0x00123D95 File Offset: 0x00121F95
		// (set) Token: 0x0600531C RID: 21276 RVA: 0x00123D9D File Offset: 0x00121F9D
		public string MethodName
		{
			get
			{
				return this.methodName;
			}
			set
			{
				this.methodName = value;
			}
		}

		// Token: 0x17000DC4 RID: 3524
		// (get) Token: 0x0600531D RID: 21277 RVA: 0x00123DA6 File Offset: 0x00121FA6
		// (set) Token: 0x0600531E RID: 21278 RVA: 0x00123DAE File Offset: 0x00121FAE
		public string XmlNameSpace
		{
			get
			{
				return this.xmlNameSpace;
			}
			set
			{
				this.xmlNameSpace = value;
			}
		}

		// Token: 0x17000DC5 RID: 3525
		// (get) Token: 0x0600531F RID: 21279 RVA: 0x00123DB7 File Offset: 0x00121FB7
		// (set) Token: 0x06005320 RID: 21280 RVA: 0x00123DBF File Offset: 0x00121FBF
		public Header[] Headers
		{
			get
			{
				return this.headers;
			}
			set
			{
				this.headers = value;
			}
		}

		// Token: 0x040024D7 RID: 9431
		internal string[] paramNames;

		// Token: 0x040024D8 RID: 9432
		internal object[] paramValues;

		// Token: 0x040024D9 RID: 9433
		internal Type[] paramTypes;

		// Token: 0x040024DA RID: 9434
		internal string methodName;

		// Token: 0x040024DB RID: 9435
		internal string xmlNameSpace;

		// Token: 0x040024DC RID: 9436
		internal Header[] headers;
	}
}
