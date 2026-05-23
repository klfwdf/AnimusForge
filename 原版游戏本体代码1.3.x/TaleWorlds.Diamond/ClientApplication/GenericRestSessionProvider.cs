using System;
using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication
{
	// Token: 0x02000046 RID: 70
	public class GenericRestSessionProvider<T> : IClientSessionProvider<T> where T : Client<T>
	{
		// Token: 0x060001A8 RID: 424 RVA: 0x00005660 File Offset: 0x00003860
		public GenericRestSessionProvider(string address, IHttpDriver httpDriver)
		{
			this._address = address;
			this._httpDriver = httpDriver;
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x00005676 File Offset: 0x00003876
		public IClientSession CreateSession(T session)
		{
			return new ClientRestSession(session, this._address, this._httpDriver);
		}

		// Token: 0x04000097 RID: 151
		private string _address;

		// Token: 0x04000098 RID: 152
		private IHttpDriver _httpDriver;
	}
}
