using System;
using System.Security.Principal;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200088F RID: 2191
	[Serializable]
	internal class CallContextSecurityData : ICloneable
	{
		// Token: 0x17000FF7 RID: 4087
		// (get) Token: 0x06005CDC RID: 23772 RVA: 0x001459DB File Offset: 0x00143BDB
		// (set) Token: 0x06005CDD RID: 23773 RVA: 0x001459E3 File Offset: 0x00143BE3
		internal IPrincipal Principal
		{
			get
			{
				return this._principal;
			}
			set
			{
				this._principal = value;
			}
		}

		// Token: 0x17000FF8 RID: 4088
		// (get) Token: 0x06005CDE RID: 23774 RVA: 0x001459EC File Offset: 0x00143BEC
		internal bool HasInfo
		{
			get
			{
				return this._principal != null;
			}
		}

		// Token: 0x06005CDF RID: 23775 RVA: 0x001459F8 File Offset: 0x00143BF8
		public object Clone()
		{
			return new CallContextSecurityData
			{
				_principal = this._principal
			};
		}

		// Token: 0x040029E3 RID: 10723
		private IPrincipal _principal;
	}
}
