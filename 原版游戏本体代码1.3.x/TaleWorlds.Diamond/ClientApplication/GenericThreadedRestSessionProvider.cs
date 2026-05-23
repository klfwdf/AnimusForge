using System;
using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication
{
	// Token: 0x02000047 RID: 71
	public class GenericThreadedRestSessionProvider<T> : IClientSessionProvider<T> where T : Client<T>
	{
		// Token: 0x060001AA RID: 426 RVA: 0x0000568F File Offset: 0x0000388F
		public GenericThreadedRestSessionProvider(string address, IHttpDriver httpDriver)
		{
			this._address = address;
			this._httpDriver = httpDriver;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x000056A8 File Offset: 0x000038A8
		public IClientSession CreateSession(T client)
		{
			int threadSleepTime;
			if (!client.Application.Parameters.TryGetParameterAsInt("ThreadedClientSession.ThreadSleepTime", out threadSleepTime))
			{
				threadSleepTime = 100;
			}
			ThreadedClient threadedClient = new ThreadedClient(client);
			ClientRestSession session = new ClientRestSession(threadedClient, this._address, this._httpDriver);
			return new ThreadedClientSession(threadedClient, session, threadSleepTime);
		}

		// Token: 0x04000099 RID: 153
		public const int DefaultThreadSleepTime = 100;

		// Token: 0x0400009A RID: 154
		private string _address;

		// Token: 0x0400009B RID: 155
		private IHttpDriver _httpDriver;
	}
}
