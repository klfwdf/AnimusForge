using System;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000B4 RID: 180
	public interface IHttpRequestTask
	{
		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x060006B8 RID: 1720
		HttpRequestTaskState State { get; }

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x060006B9 RID: 1721
		bool Successful { get; }

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x060006BA RID: 1722
		string ResponseData { get; }

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x060006BB RID: 1723
		Exception Exception { get; }

		// Token: 0x060006BC RID: 1724
		void Start();
	}
}
