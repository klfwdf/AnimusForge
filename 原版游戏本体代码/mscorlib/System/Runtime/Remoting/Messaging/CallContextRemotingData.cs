using System;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000890 RID: 2192
	[Serializable]
	internal class CallContextRemotingData : ICloneable
	{
		// Token: 0x17000FF9 RID: 4089
		// (get) Token: 0x06005CE1 RID: 23777 RVA: 0x00145A20 File Offset: 0x00143C20
		// (set) Token: 0x06005CE2 RID: 23778 RVA: 0x00145A28 File Offset: 0x00143C28
		internal string LogicalCallID
		{
			get
			{
				return this._logicalCallID;
			}
			set
			{
				this._logicalCallID = value;
			}
		}

		// Token: 0x17000FFA RID: 4090
		// (get) Token: 0x06005CE3 RID: 23779 RVA: 0x00145A31 File Offset: 0x00143C31
		internal bool HasInfo
		{
			get
			{
				return this._logicalCallID != null;
			}
		}

		// Token: 0x06005CE4 RID: 23780 RVA: 0x00145A3C File Offset: 0x00143C3C
		public object Clone()
		{
			return new CallContextRemotingData
			{
				LogicalCallID = this.LogicalCallID
			};
		}

		// Token: 0x040029E4 RID: 10724
		private string _logicalCallID;
	}
}
